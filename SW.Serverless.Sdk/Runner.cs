using Newtonsoft.Json;
using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SW.Serverless.Sdk
{
    public static class Runner
    {
        public static ServerlessOptions ServerlessOptions { get; private set; }
        public static IReadOnlyDictionary<string, string> StartupValues { get; private set; }
        public static IReadOnlyDictionary<string, string> AdapterValues { get; private set; }

        public static void MockRun(object commandHandler, ServerlessOptions serverlessOptions, IReadOnlyDictionary<string, string> startupValues = null, IReadOnlyDictionary<string, string> adapterValues = null)
        {

            ServerlessOptions = serverlessOptions;
            StartupValues = startupValues;
            AdapterValues = adapterValues;

            BuildMethodsDictionary(commandHandler);
        }

        async public static Task Run(object commandHandler)
        {
            Timer idleTimer = null;

            try
            {
                Console.InputEncoding = Encoding.UTF8;
                Console.OutputEncoding = Encoding.UTF8;
                var commandLineArgs = Environment.GetCommandLineArgs();

                try
                {
                    ServerlessOptions = JsonConvert.DeserializeObject<ServerlessOptions>(Encoding.UTF8.GetString(Convert.FromBase64String(commandLineArgs[1])));
                }
                catch (Exception ex)
                {
                    AdapterLogger.LogWarning(ex, $"Failed to parse ServerlessOptions, using defaults instead.");
                    ServerlessOptions = new ServerlessOptions();
                }

                try
                {
                    StartupValues = new Dictionary<string, string>(JsonConvert.DeserializeObject<IDictionary<string, string>>(Encoding.UTF8.GetString(Convert.FromBase64String(commandLineArgs[2]))), StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    AdapterLogger.LogWarning(ex, $"Failed to parse StartupValues.");
                }

                try
                {
                    AdapterValues = new Dictionary<string, string>(JsonConvert.DeserializeObject<IDictionary<string, string>>(Encoding.UTF8.GetString(Convert.FromBase64String(commandLineArgs[3]))), StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    AdapterLogger.LogWarning(ex, $"Failed to parse AdapterValues.");
                }

                var methodsDictionary = BuildMethodsDictionary(commandHandler);

                while (true)
                {

                    idleTimer = new Timer(
                        callback: state =>
                        {
                            idleTimer.Dispose();
                            throw new TimeoutException("Timed out waiting for command.");
                        },
                        state: null,
                        dueTime: TimeSpan.FromSeconds(ServerlessOptions.IdleTimeout),
                        period: Timeout.InfiniteTimeSpan);

                    var input = await Console.In.ReadLineAsync();

                    try
                    {
                        idleTimer.Dispose();

                        if (input == Constants.QuitCommand) break;
                        if (input == null) continue;

                        var inputSegments = input.Split(Constants.Delimiter);

                        if (inputSegments.Length != 4)
                            throw new Exception("Wrong data format.");

                        object result = null;

                        if (methodsDictionary.TryGetValue(inputSegments[1], out var handlerMethodInfo))
                        {
                            //string inputDenormalized = null;
                            object inputTyped = null;

                            if (inputSegments[2] != Constants.NullIdentifier && handlerMethodInfo.ParameterType != null)
                            {
                                var inputDenormalized = inputSegments[2].Replace(Constants.NewLineIdentifier, "\n");
                                if (handlerMethodInfo.ParameterType == typeof(string))
                                    inputTyped = inputDenormalized;
                                else if (handlerMethodInfo.ParameterType.IsPrimitive)
                                    inputTyped = Convert.ChangeType(inputDenormalized, handlerMethodInfo.ParameterType);
                                else
                                    inputTyped = JsonConvert.DeserializeObject(inputDenormalized, handlerMethodInfo.ParameterType);
                            }

                            if (handlerMethodInfo.Void)
                            {
                                if (handlerMethodInfo.ParameterType == null)
                                    await (Task)handlerMethodInfo.MethodInfo.Invoke(commandHandler, null);
                                else
                                    await (Task)handlerMethodInfo.MethodInfo.Invoke(commandHandler, new object[] { inputTyped });
                            }
                            else
                            {
                                if (handlerMethodInfo.ParameterType == null)
                                    result = await (Task<object>)handlerMethodInfo.MethodInfo.Invoke(commandHandler, null);
                                else
                                    result = await (Task<object>)handlerMethodInfo.MethodInfo.Invoke(commandHandler, new object[] { inputTyped });
                            }

                            string resultString = null;

                            if (result == null)
                                resultString = Constants.NullIdentifier;
                            else if (result.GetType() == typeof(string) || result.GetType().IsPrimitive)
                                resultString = result.ToString();
                            else
                                resultString = JsonConvert.SerializeObject(result);

                            await Console.Out.WriteLineAsync($"{Constants.Delimiter}{resultString.Replace("\n", Constants.NewLineIdentifier).Replace("\r", "")}{Constants.Delimiter}");
                        }

                        else
                            throw new MissingMethodException(commandHandler.GetType().FullName, inputSegments[1]);
                    }
                    catch (Exception ex)
                    {
                        await Console.Out.WriteLineAsync($"{Constants.ErrorIdentifier}{ex.ToString().Replace("\n", Constants.NewLineIdentifier).Replace("\r", "")}");
                    }
                };

            }
            catch (Exception ex)
            {
                AdapterLogger.LogError(ex, "Terminal error.");
            }
            finally
            {
                idleTimer?.Dispose();
            }
        }

        static Dictionary<string, HandlerMethodInfo> BuildMethodsDictionary(object commandHandler)
        {
            var methodsDictionary = new Dictionary<string, HandlerMethodInfo>(StringComparer.OrdinalIgnoreCase);

            var methods = commandHandler.GetType().
                GetMethods(BindingFlags.Instance | BindingFlags.Public).
                Where(m =>
                    !m.IsGenericMethod &&
                    m.GetParameters().Length <= 1).ToList();

            methods.Where(m => m.ReturnType == typeof(Task) && m.GetParameters().Length == 0). //|| (m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)))).
                ToList().
                ForEach(m => methodsDictionary.Add(m.Name, new HandlerMethodInfo
                {
                    MethodInfo = m,
                    Void = true,

                }));

            methods.Where(m => m.ReturnType == typeof(Task) && m.GetParameters().Length == 1).
                ToList().
                ForEach(m => methodsDictionary.Add(m.Name, new HandlerMethodInfo
                {
                    MethodInfo = m,
                    Void = true,
                    ParameterType = m.GetParameters()[0].ParameterType

                }));

            methods.Where(m => m.ReturnType == typeof(Task<object>) && m.GetParameters().Length == 0).
                ToList().
                ForEach(m => methodsDictionary.Add(m.Name, new HandlerMethodInfo
                {
                    MethodInfo = m,
                    Void = false,
                }));

            methods.Where(m => m.ReturnType == typeof(Task<object>) && m.GetParameters().Length == 1).
                ToList().
                ForEach(m => methodsDictionary.Add(m.Name, new HandlerMethodInfo
                {
                    MethodInfo = m,
                    Void = false,
                    ParameterType = m.GetParameters()[0].ParameterType
                }));

            return methodsDictionary;
        }
    }
}
