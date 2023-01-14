using NBG.Automation.RuntimeTests;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Automation.Core
{
    internal class RuntimeTestServer
    {
        AutoEnv _env;
        IWebHost _webHost;
        CancellationTokenSource _cts = new CancellationTokenSource();
        Task _task;

        public RuntimeTestServer(AutoEnv env)
        {
            _env = env;
        }

        public event Action<string, bool> OnLogMessage;
        public event Action<string, string> OnHello;
        public event Action<string> OnArtifactsPath;
        public event Action<string, string, TestStatus, string, string> OnReportTest;
        public event Action<int> OnReportTestRunResult;
        public event Action<int> OnSetTimeout;
        public event Action<string> OnTakeSystemScreenshot;

        public void Run()
        {
            _webHost = new WebHostBuilder()
                .UseKestrel(opts =>
                {
                    opts.ListenAnyIP(Settings.Port, listen =>
                    {
                        listen.NoDelay = true;
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddSingleton(typeof(AutoEnv), _env);
                })
                .Configure(app =>
                {
                    app.UseRouter(r =>
                    {
                        r.MapPost("log", async ctx =>
                        {
                            var buildStatus = false;
                            if (ctx.Request.Form.ContainsKey("status"))
                            {
                                if (ctx.Request.Form["status"][0] == "1")
                                    buildStatus = true;
                            }

                            if (ctx.Request.Form.TryGetValue("message", out StringValues values))
                            {
                                foreach (var value in values)
                                {
                                    Utils.Log.Message($"POST log message: {value}");
                                    OnLogMessage?.Invoke(value, buildStatus);
                                }
                                ctx.Response.StatusCode = StatusCodes.Status200OK;
                            }
                            else
                            {
                                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                            }
                            await Task.Yield();
                        });

                        r.MapPost("hello", async ctx =>
                        {
                            var companyName = ctx.Request.Form["companyName"][0];
                            var productName = ctx.Request.Form["productName"][0];
                            Utils.Log.Message($"POST hello: {companyName}, {productName}");
                            OnHello?.Invoke(companyName, productName);
                            ctx.Response.StatusCode = StatusCodes.Status200OK;

                            await Task.Yield();
                        });

                        r.MapPost("artifactsPath", async ctx =>
                        {
                            if (ctx.Request.Form.TryGetValue("value", out StringValues values))
                            {
                                Utils.Log.Message($"POST artifactsPath: {values[0]}");
                                OnArtifactsPath?.Invoke(values[0]);
                                ctx.Response.StatusCode = StatusCodes.Status200OK;
                            }
                            else
                            {
                                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                            }
                            await Task.Yield();
                        });

                        r.MapPost("timeout", async ctx =>
                        {
                            if (ctx.Request.Form.TryGetValue("value", out StringValues values))
                            {
                                var timeout = int.Parse(values[0]);
                                Utils.Log.Message($"POST timeout: {timeout}");
                                OnSetTimeout?.Invoke(timeout);
                                ctx.Response.StatusCode = StatusCodes.Status200OK;
                            }
                            else
                            {
                                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                            }
                            await Task.Yield();
                        });

                        r.MapPost("reportTest", async ctx =>
                        {
                            var suiteName = ctx.Request.Form["suiteName"][0];
                            var testName = ctx.Request.Form["testName"][0];
                            var status = Enum.Parse<TestStatus>(ctx.Request.Form["status"][0]);
                            var message = ctx.Request.Form.ContainsKey("message") ? ctx.Request.Form["message"][0] : string.Empty;
                            var details = ctx.Request.Form.ContainsKey("details") ? ctx.Request.Form["details"][0] : string.Empty;
                            Utils.Log.Message($"POST reportTest: {suiteName}, {testName}, {status}, {message}, {details}");
                            OnReportTest?.Invoke(suiteName, testName, status, message, details);
                            ctx.Response.StatusCode = StatusCodes.Status200OK;
                            
                            await Task.Yield();
                        });

                        r.MapPost("reportTestRunResult", async ctx =>
                        {
                            if (ctx.Request.Form.TryGetValue("value", out StringValues values))
                            {
                                var returnCode = int.Parse(values[0]);
                                Utils.Log.Message($"POST reportTestRunResult (returnCode: {returnCode})");
                                OnReportTestRunResult?.Invoke(returnCode);
                                ctx.Response.StatusCode = StatusCodes.Status200OK;
                            }
                            else
                            {
                                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                            }
                            await Task.Yield();
                        });

                        r.MapPost("takeSystemScreenshot", async ctx =>
                        {
                            if (ctx.Request.Form.TryGetValue("value", out StringValues values))
                            {
                                var name = values[0];
                                Utils.Log.Message($"POST takeSystemScreenshot: {name}");
                                OnTakeSystemScreenshot?.Invoke(name);
                                ctx.Response.StatusCode = StatusCodes.Status200OK;
                            }
                            else
                            {
                                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                            }
                            await Task.Yield();
                        });
                    });

                    app.Run(context =>
                    {
                        return context.Response.WriteAsync("No Brakes Games runtime test server");
                    });
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole(opts =>
                    {
                        opts.LogToStandardErrorThreshold = LogLevel.Warning;
                    });
                })
                .Build();

            _task = _webHost.RunAsync(_cts.Token);
        }

        public async Task Stop()
        {
            _cts.Cancel();

            try
            {
                await _task;
            }
            catch (OperationCanceledException e)
            {
                Utils.Log.Message($"{nameof(OperationCanceledException)} thrown with message: {e.Message}");
            }
            finally
            {
                _cts.Dispose();
            }
        }
    }
}
