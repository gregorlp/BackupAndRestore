using AdoNetCore.AseClient;
using System;
using System.IO;
using System.Text;
using Dapper;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace BacakupAndRestore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var ConnectionSybase = "Data Source=10.1.1.36;Port=5000;Database=cafardb;Uid=sa;Pwd=cafsyb;Charset=cp850;";

            var ConnectionMaster = "Data Source=10.1.4.10;Port=5000;Database=master;Uid=sa;Pwd=cafsybweb;Charset=cp850;";

            var logger = NLog.Web.NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            logger.Info("Prueba - Iniciando Backup and Restore diario");
            Console.WriteLine("Prueba - Iniciando Backup and Restore diario");

            using (Process p = Process.GetCurrentProcess())
                p.PriorityClass = ProcessPriorityClass.High;

            try
            {
                using (AseConnection _connectionSybase = new AseConnection(ConnectionSybase))
                {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    try
                    {
                        _connectionSybase.Open();
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Error al conectarse con RDC-W2K12");
                        Console.WriteLine("Error al conectarse con RDC-W2K12");
                        logger.Error(ex.Message);
                        Console.WriteLine(ex.Message);
                    }
                    if (_connectionSybase.State == System.Data.ConnectionState.Open)
                    {
                        var backupDiario = "cafardb" + DateTime.Now.Date.ToString("yyyyMMdd");

                        var pathBackupProd = "C:\\Users\\Administrator\\Desktop\\COMPARTIDO RDC-W2K12\\" + backupDiario;

                        logger.Info("Conexión establecida");
                        Console.WriteLine("Conexión establecida");
                        if (File.Exists(pathBackupProd))
                        {
                            logger.Error("Ya existe " + pathBackupProd);
                            Console.WriteLine("Ya existe " + pathBackupProd);
                        }
                        else
                        {

                            var sqlBackup = "dump database cafardb to " + '"' + pathBackupProd + '"' + " with notify = client";

                            var command = _connectionSybase.CreateCommand();

                            command.CommandText = sqlBackup;

                            logger.Info("Iniciando dump");
                            Console.WriteLine("Iniciando dump");
                            try
                            {

                                void captureResult(object sender, AseInfoMessageEventArgs aseInfoMessageEventArgs)
                                {
                                    Console.WriteLine(aseInfoMessageEventArgs.Message);
                                }

                                _connectionSybase.InfoMessage += captureResult;

                                command.ExecuteNonQuery();

                                logger.Info("Dump finalizado");
                                Console.WriteLine("Dump finalizado");

                                var pathBackupWeb = "\\\\10.1.2.10\\compartidosybase15\\" + backupDiario;

                                if (File.Exists(pathBackupWeb))
                                {
                                    logger.Error("Ya existe " + pathBackupWeb);
                                    Console.WriteLine("Ya existe " + pathBackupWeb);
                                }
                                else
                                {
                                    logger.Info("Copiando backup en SYBASE15");
                                    Console.WriteLine("Copiando backup en SYBASE15");
                                    File.Copy(pathBackupProd, pathBackupWeb);

                                    logger.Info("Copia de backup finalizada");
                                    Console.WriteLine("Copia de backup finalizada");
                                    try
                                    {

                                        using (AseConnection connectionMaster = new AseConnection(ConnectionMaster))
                                        {

                                            logger.Info("Conectando a Sybase15");
                                            Console.WriteLine("Conectando a Sybase15");
                                            var sqlRestore = "load database CafarWeb from " + '"' + pathBackupWeb + '"';

                                            connectionMaster.Open();

                                            logger.Info("Conexion Establecida con Sybase15");
                                            Console.WriteLine("Conexion Establecida con Sybase15");
                                            var procesos = connectionMaster.Query<Procces>("sp_who sa").ToList();

                                            var procesoSaCafarNov = procesos.Where(x => x.dbname == "CafarNov" || x.dbname == "CafarWeb");
                                            if (procesoSaCafarNov.Any())
                                            {
                                                logger.Info("Limpiando procesos sa en SYBASE15");
                                                Console.WriteLine("Limpiando procesos sa en SYBASE15");

                                            }
                                            foreach (Procces proceso in procesoSaCafarNov)
                                            {
                                                try
                                                {
                                                    connectionMaster.Execute("kill " + proceso.spid);
                                                }
                                                catch (Exception ex)
                                                {
                                                    Console.WriteLine(ex.Message);
                                                    logger.Error(ex.Message);
                                                }

                                            }

                                            //_connectionWeb.Open();

                                            logger.Info("Iniciando Restore");
                                            Console.WriteLine("Iniciando Restore");
                                            connectionMaster.Execute(sqlRestore);

                                            logger.Info("Restore Finalizado en Web");
                                            Console.WriteLine("Restore Finalizado en Web");
                                            connectionMaster.Execute("Online database CafarWeb");

                                            logger.Info("Bases de Datos puesta OnLine");
                                            Console.WriteLine("Bases de Datos puesta OnLine");
                                            connectionMaster.Close();
                                            connectionMaster.Dispose();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.Error(ex.Message);
                                        Console.WriteLine(ex.Message);
                                    }


                                }

                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex.Message);
                                Console.WriteLine(ex.Message);
                            }

                        }

                        _connectionSybase.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);

                logger.Error("Error");

                Console.WriteLine(ex.Message);
            }
        }
    }
}
