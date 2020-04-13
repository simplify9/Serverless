using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SW.Serverless.Sdk
{
    public sealed class Runner
    {
        async public static Task Run(object commandHandler)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            //using var standardError = new StreamWriter(Console.OpenStandardError());
            //standardError.AutoFlush = true;
            //Console.SetError(standardError);


            var methodsDictionary = new Dictionary<string, HandlerMethodInfo>(StringComparer.OrdinalIgnoreCase);

            var methods = commandHandler.GetType().
                GetMethods(BindingFlags.Instance | BindingFlags.Public).
                Where(m =>
                    !m.IsGenericMethod &&
                    m.GetParameters().Length == 1).ToList();

            methods.Where(m => m.ReturnType == typeof(Task)). //|| (m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)))).
                ToList().
                ForEach(m => methodsDictionary.Add(m.Name, new HandlerMethodInfo
                {
                    MethodInfo = m,
                    Void = true
                }));

            methods.Where(m => m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>) && m.ReturnType.GetGenericArguments()[0] == typeof(string)).
                ToList().
                ForEach(m => methodsDictionary.Add(m.Name, new HandlerMethodInfo
                {
                    MethodInfo = m,
                    Void = false
                }));

            while (true)
            {
                var input = await Console.In.ReadLineAsync();
                try
                {
                    var inputSegments = input.Split(Constants.Delimiter);

                    if (inputSegments.Length != 4)
                        throw new Exception("Wrong data format.");

                    string result = Constants.NullIdentifier;

                    if (methodsDictionary.TryGetValue(inputSegments[1], out var handlerMethodInfo))
                    {
                        var inputDenormalized = inputSegments[2].Replace(Constants.NewLineIdentifier, "\n");

                        if (handlerMethodInfo.Void)
                            await (Task)handlerMethodInfo.MethodInfo.Invoke(commandHandler, new object[] { inputDenormalized });
                        else
                            result = await (Task<string>)handlerMethodInfo.MethodInfo.Invoke(commandHandler, new object[] { inputDenormalized });

                        if (result == null)
                            result = Constants.NullIdentifier;

                        await Console.Out.WriteLineAsync($"{Constants.Delimiter}{result.Replace("\n", Constants.NewLineIdentifier)}{Constants.Delimiter}");
                    }

                    else
                        throw new NotSupportedException();
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync($"{Constants.ErrorIdentifier}{ex.ToString().Replace("\n", Constants.NewLineIdentifier)}");
                }
            };
        }
    }
}
