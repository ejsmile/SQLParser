using CommandLine;
using NLog;
using System;
using System.IO;
using System.Linq;

namespace SQLParser
{
    class Program
    {
        static void Main(string[] args)
        {
            var log = LogManager.GetCurrentClassLogger();

            Parser.Default.ParseArguments<Options>(args)
                  .WithParsed(o =>
                  {
                      log.Info("Init");
                      var ok = true;
                      try
                      {
                          using (var runner = new SQLRunner(o.ConnectionString, o.Timeout))
                          {
                              runner.Connect();
                              var filesName = o.FileName.Split(';').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

                              foreach (var fileName in filesName)
                              {
                                  if (!File.Exists(fileName))
                                  {
                                      log.Error($"File not found '{fileName}'");
                                      Environment.Exit(1);
                                  }
                                  try
                                  {
                                      var result = runner.RunSQLFile(fileName);
                                      if (!result)
                                      {
                                          log.Error($"Error on SQL {fileName}");
                                      }
                                      ok &= result;

                                  }
                                  catch (Exception ex)
                                  {
                                      log.Error(ex, "Error on SQL");
                                      Environment.Exit(1);
                                  }

                              }
                          }
                      }
                      catch (Exception ex)
                      {
                          log.Error(ex, "Error on Connect");
                          Environment.Exit(1);
                      }
                      log.Info("finish");
                      Environment.Exit(ok ? 0 : 1);
                  });
        }


    }
}
