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
    public sealed class Runner
    {
        public static ServerlessOptions ServerlessOptions { get; private set; }
        public static IReadOnlyDictionary<string, string> StartupValues { get; private set; }
        public static IReadOnlyDictionary<string, string> AdapterValues { get; private set; }

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
                        Parameterless = true

                    }));

                methods.Where(m => m.ReturnType == typeof(Task) && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string)).
                    ToList().
                    ForEach(m => methodsDictionary.Add(m.Name, new HandlerMethodInfo
                    {
                        MethodInfo = m,
                        Void = true,
                        Parameterless = false

                    }));

                methods.Where(m => m.ReturnType == typeof(Task<string>) && m.GetParameters().Length == 0).
                    ToList().
                    ForEach(m => methodsDictionary.Add(m.Name, new HandlerMethodInfo
                    {
                        MethodInfo = m,
                        Void = false,
                        Parameterless = true
                    }));

                methods.Where(m => m.ReturnType == typeof(Task<string>) && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string)).
                    ToList().
                    ForEach(m => methodsDictionary.Add(m.Name, new HandlerMethodInfo
                    {
                        MethodInfo = m,
                        Void = false,
                        Parameterless = false
                    }));

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

                        string result = Constants.NullIdentifier;

                        if (methodsDictionary.TryGetValue(inputSegments[1], out var handlerMethodInfo))
                        {
                            string inputDenormalized = null;

                            if (inputSegments[2] != Constants.NullIdentifier)
                                inputDenormalized = inputSegments[2].Replace(Constants.NewLineIdentifier, "\n");

                            if (handlerMethodInfo.Void)
                            {
                                if (handlerMethodInfo.Parameterless)
                                    await (Task)handlerMethodInfo.MethodInfo.Invoke(commandHandler, null);
                                else
                                    await (Task)handlerMethodInfo.MethodInfo.Invoke(commandHandler, new object[] { inputDenormalized });
                            }
                            else
                            {
                                if (handlerMethodInfo.Parameterless)
                                    result = await (Task<string>)handlerMethodInfo.MethodInfo.Invoke(commandHandler, null);
                                else
                                    result = await (Task<string>)handlerMethodInfo.MethodInfo.Invoke(commandHandler, new object[] { inputDenormalized });
                            }

                            if (result == null)
                                result = Constants.NullIdentifier;

                            await Console.Out.WriteLineAsync($"{Constants.Delimiter}{result.Replace("\n", Constants.NewLineIdentifier).Replace("\r", "")}{Constants.Delimiter}");
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
    }
}
