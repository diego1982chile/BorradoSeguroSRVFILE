using Castle.Core.Logging;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;


namespace cl.trends.pci.SRVFILE.BorradoSeguroSRVFILE
{
    class Program
    {
        static readonly String ERASER_ARGUMENTS = "erase /method=b1bfab4a-31d3-43a5-914c-e9892c78afd8 /target file=";
        static readonly String DELIMITER = "\t";
        static readonly DateTime TODAY = DateTime.Today;
        static readonly String LOG_FILE = @"log.txt";
        static List<String> PATHS = new List<string>();
        static String ERASER_PATH = @"C:\Program Files\Eraser\Eraser.exe";

        static void Main(string[] args)
        {

            try
            {
                Log(TODAY.ToString());
                readParameters();

                if (!EraserExists())
                {
                    String msg = "No se ha encontrado una instalación de Eraser con la ruta configurada " + ERASER_PATH + ". Configure correctamente la ruta o contacte al Administrador.";
                    Log(msg, EventLogEntryType.Error);
                    Log(msg);
                    throw new System.ApplicationException(msg);
                }

                foreach (String path in PATHS)
                {
                    if (!Directory.Exists(path))
                    {
                        String msg = "Se esperaba directorio " + path + ". Verifique que exista el directorio o remueva el directorio del archivo de configuración. Si el problema persiste contacte al Administrador.";
                        Log(msg, EventLogEntryType.Error);
                        Log(msg);
                        throw new System.ApplicationException(msg);
                    }

                    Erase(path);
                    
                    foreach (String directory in Directory.GetDirectories(path))
                    {
                        Directory.Delete(directory, true);
                    }                    

                }
            }
            catch (Exception e)
            {
                Log(e.Message, EventLogEntryType.Error);
                Log(e.Message);
                throw e;
            }

            System.Environment.Exit(1);
        }

        static void Erase(String path)
        {
            foreach (String file in Directory.GetFiles(path))
            {
                CallEraserOnFile(file);
            }

            foreach (String directory in Directory.GetDirectories(path))
            {
                Erase(directory);
            }
        }

        static void readParameters()
        {
            try
            {
                FileStream fileStream = new FileStream("parameters.txt", FileMode.Open);

                using (StreamReader reader = new StreamReader(fileStream))
                {
                    while (reader.Peek() >= 0)
                    {
                        string line = reader.ReadLine();

                        string[] tokens = line.Split('=');

                        if (tokens.Length != 2)
                        {
                            String msg = "Formato no válido. Los parámetros deben ser especificados en la forma: [NOMBRE] = [VALOR]";
                            Log(msg, EventLogEntryType.Error);
                            throw new System.ApplicationException(msg);
                        }

                        switch (tokens[0])
                        {
                            case "ERASER_HOME":
                                ERASER_PATH = tokens[1] + "Eraser.exe";
                                break;
                            case "PATHS":
                                string[] tokens2 = tokens[1].Split(',');
                                foreach (String path in tokens2)
                                {
                                    PATHS.Add(path);
                                }
                                break;
                            default:
                                String msg = "Parámetro no válido. Valores aceptados: ERASER_HOME, PATHS";
                                Log(msg, EventLogEntryType.Error);
                                Log(msg);
                                throw new System.ApplicationException(msg);
                        }

                    }
                }
            }
            catch (FileNotFoundException e)
            {
                String msg = "El archivo de parámetros 'parameters.txt' no existe. Debe crear este archivo en la ruta donde se encuentra el ejecutable del aplicativo.";
                Log(msg, EventLogEntryType.Error);
                Log(msg);
                throw new System.ApplicationException(msg);
            }
            catch (FormatException e2)
            {
                String msg = "Formato no válido. Los parámetros deben ser especificados en la forma: [NOMBRE] = [VALOR]";
                Log(msg, EventLogEntryType.Error);
                Log(msg);
                throw new System.ApplicationException(msg);
            }
        }

        static Boolean EraserExists()
        {
            return File.Exists(ERASER_PATH);
        }

        static void CallEraserOnFile(String path)
        {
            try
            {
                Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = ERASER_PATH;
                //process.StartInfo.Arguments =ERASER_ARGUMENTS.Replace("?", path); //argument
                process.StartInfo.Arguments = ERASER_ARGUMENTS + '"' + path + '"'; //argument
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = true; //not diplay a windows
                process.StartInfo.Verb = "runas";
                process.Start();
                //process.StandardOutput.ReadToEnd();
                //process.WaitForExit();
                while (File.Exists(path))
                {
                    Thread.Sleep(2000);
                }
                if (!process.HasExited)
                {
                    process.Kill();
                }
                string output = "Contenido del directorio " + path + " borrado exitosamente."; //The output result                
                Log(output, EventLogEntryType.Information);
                Log(path + DELIMITER + "OK");
            }
            catch (Exception e)
            {
                Log(e.Message, EventLogEntryType.Information);
                Log(path + DELIMITER + "FALLÓ");
                //throw new System.ApplicationException(e.Message);
            }

        }

        static void Log(String message, EventLogEntryType level)
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = "cl.trends.pci.SRVFILE.BorradoSeguroSRVFILE";
                eventLog.WriteEntry(message, level, 9998, 19 /*Archive Task*/);
            }
        }

        static void Log(String message)
        {
            using (StreamWriter sw = File.AppendText(LOG_FILE))
            {
                sw.WriteLine(message);
            }
        }
    }
}
