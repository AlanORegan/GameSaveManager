using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace GameSaveManager
{
    static class Program
    {
        private const string PipeName = "GameSaveManagerPipe";

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ApplicationConfiguration.Initialize();

            // This section allows the application to be invoked from a shortcut with the --restore argument
            // and will restore the most recent backup silently, either
            // - without showing the main form if it is not running, or
            // - by sending a command to the already running instance to perform the restore.

            // Check if the invocation has arguments (e.g., --restore or --backup)
            bool callHasCommand = args != null && args.Length > 0;

            // Ensure only one instance of the application runs at a time using a named mutex
            bool thisIsTheFirstInstance;
            using (var mutex = new System.Threading.Mutex(true, "GameSaveManagerMutex", out thisIsTheFirstInstance))
            {
                if (thisIsTheFirstInstance)
                {
                    var mainForm = new MainForm();

                    // If there is no command, start named pipe server in background for IPC from a later command
                    if (!callHasCommand)
                    {
                        var pipeThread = new Thread(() =>
                        {
                            while (true)
                            {
                                using (var server = new NamedPipeServerStream(PipeName, PipeDirection.In))
                                {
                                    server.WaitForConnection();
                                    using (var reader = new StreamReader(server))
                                    {
                                        var command = reader.ReadLine();
                                        if (command == "--restore")
                                        {
                                            mainForm.Invoke(new Action(() =>
                                            {
                                                mainForm.RestoreMostRecentBackupSilent();
                                            }));
                                        }
                                        else if (command == "--backup")
                                        {
                                            mainForm.Invoke(new Action(() =>
                                            {
                                                mainForm.BackupGameSilent();
                                            }));
                                        }
                                    }
                                }
                            }
                        });
                        pipeThread.IsBackground = true;
                        pipeThread.Start();
                    }
                    else
                    {
                        // If launched with a command like --restore or --backup, perform silent operation and exit
                        string command = args[0].ToLower();
                        if (command == "--restore" || command == "--backup")
                        {
                            mainForm.LoadGameConfigs();
                            if (command == "--restore")
                                mainForm.RestoreMostRecentBackupSilent();
                            else
                                mainForm.BackupGameSilent();
                            Thread.Sleep(2000);
                            return;
                        }
                    }

                    Application.Run(mainForm);
                }
                else
                {
                    // Another instance is running: send command via named pipe
                    if (callHasCommand)
                    {
                        string command = args[0].ToLower();
                        if (command == "--restore" || command == "--backup")
                        {
                            try
                            {
                                using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                                {
                                    client.Connect(2000); // Wait up to 2 seconds
                                    using (var writer = new StreamWriter(client))
                                    {
                                        writer.WriteLine(command);
                                        writer.Flush();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Could not send " + command + " to running instance.\n" + ex.Message, "IPC Error");
                            }
                        }
                        Thread.Sleep(1000);
                        return;
                    }
                    else
                    {
                        MessageBox.Show("Game Save Manager is already running.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
        }
    }
}