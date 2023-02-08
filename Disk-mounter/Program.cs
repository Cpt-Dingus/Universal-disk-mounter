using System.Diagnostics;


/*
 * Made by Cpt-Dingus
 * v0.4 - 09/02/2023
 * CLI VERSION
 */




namespace disk_mounter
{
    class program
    {

        // -- Functions --
        public static Tuple<string, int> run_cmd(string cmd)
        {
            Process process = new Process();
            process.StartInfo.FileName = "powershell.exe";
            process.StartInfo.Arguments = $"-command \"{cmd}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return Tuple.Create(output, process.ExitCode);
        }


        public static int run_elevated_cmd(string cmd, string args)
        {
            // Doesn't have STDOUT because csharp is dumb and can't have UsesShellExecute set
            // to true with redirected STDOUT, but needs it to be true for it to run elevated

            Process proc = new Process();
            proc.StartInfo.FileName = cmd;
            proc.StartInfo.Arguments = args;
            proc.StartInfo.Verb = "runas";
            proc.StartInfo.UseShellExecute = true;

            proc.Start();

            proc.WaitForExit();
            return proc.ExitCode;


        }

        static void Main(string[] args)
        {

            // -- Vars -- 

            string disk_number,
                   disk_label = "NULL",
                   drive_letter = "",
                   disk_list,
                   subst_output = "",
                   param_contents = "NONE";



            string[] yes = { "y", "yes" },
                     mount = { "m", "mount" },
                     unmount = { "u", "unmount" },
                     quit = { "q", "quit", "null" },
                     alphabet = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };

            bool dtc = true;



            //TODO: Distro Config

            // Base checks

            Console.WriteLine("Running base checks...");
            const string wsl_dir = @"C:\Windows\System32\wsl.exe";
            if (!File.Exists(wsl_dir))
            {
                Console.WriteLine(@"WSL executable not found! Please make sure it is installed and enabled at C:\Windows\System32\wsl.exe");
                run_cmd("PAUSE"); Environment.Exit(1);
            }


            // This borked between builds, I have no damn idea why. Will leave it's fate up to god
            /*
            else if (!Directory.Exists(@"\\WSL$\"))
            {
                Console.WriteLine(@"\\WSL$\ not found!");
                run_cmd("PAUSE"); Environment.Exit(1);
            }*/

            else if (run_cmd("net session").Item2 == 0)
            {
                Console.WriteLine("Detected elevated permissions, this program has to be run from an unelevated shell!");
                run_cmd("PAUSE"); Environment.Exit(1);
            }

            // Clears base checks from history
            run_cmd("cls");

            // -- Main  --

            while (true)
            {
                // MODE SELECT
                Console.Write("Mount mode [M,Mount] OR Unmount mode [U,Unmomunt]?\n>");
                Console.SetCursorPosition(2, Console.CursorTop);
                string mode = Console.ReadLine() ?? "NULL";




                // MOUNT MODE

                if (mount.Contains(mode.ToLower()))
                {

                    // - Disk labels & \\.\PHYSICALDRIVE path -
                    Console.WriteLine("Getting disks...");

                    // Later used for disk label confirmation
                    disk_list = run_cmd("GET-CimInstance -query \\\"SELECT DeviceID,Caption from Win32_DiskDrive\\\" | Select-Object DeviceID,Caption | Sort-Object DeviceID").Item1;
                    Console.WriteLine(disk_list);



                    while (true)
                    {
                        Console.Write("Select disk number:\n>");
                        Console.SetCursorPosition(2, Console.CursorTop);
                        disk_number = Console.ReadLine() ?? "NULL"; // Later used for SUBST command


                        // Gets selected disk label

                        foreach (string line in disk_list.Split('\n'))
                        {
                            string[] line_split = line.TrimEnd().Split(' ');
                            if (line_split[0] == $"\\\\.\\PHYSICALDRIVE{disk_number}")
                            {
                                disk_label = string.Join(" ", line_split.Skip(1).ToArray()) ?? "NULL";
                            }
                        }

                        if (disk_label == "NULL")
                        { throw new Exception("Disk label was read as NULL"); }


                        // Disk label confirmation
                        Console.WriteLine($"You selected {disk_label}, is that correct? [Y/N]\n>");
                        Console.SetCursorPosition(2, Console.CursorTop - 1);
                        string input = Console.ReadLine() ?? "NULL".ToLower();


                        if (yes.Contains(input.ToLower()))
                        {

                            while (dtc == true)
                            {
                                dtc = false; // While loop will end if no security detection is made
                                Console.WriteLine("Please select desired drive letter:\n>");
                                Console.SetCursorPosition(2, Console.CursorTop - 1);
                                drive_letter = Console.ReadLine() ?? "C"; // C will mark as invalid

                                if (!alphabet.Contains(drive_letter) || drive_letter.Length > 1 || Directory.Exists($"{drive_letter}:\\"))
                                { Console.WriteLine("Letter already in use or invalid letter inputted!"); dtc = true; }



                                else
                                {

                                    // Loops through each line of "subst" to see if one already exists for specified drive letter
                                    foreach (var line in run_cmd("subst").Item1.Split('\n')) // Splits 'subst' into lines
                                    {

                                        if (line.Split(' ').Contains($@"{drive_letter.ToUpper()}:\:"))
                                        {
                                            Console.WriteLine($"{drive_letter} already SUBST-ed!"); dtc = true; break; // If the line contains the drive letter, break out of the for loo
                                        }

                                    }

                                }
                            }
                            break;
                        }
                    }

                    Console.WriteLine($"Selected {drive_letter}:\\");



                    Console.WriteLine("Mounting Disk to WSL (This requires administrative privileges)...");
                    run_elevated_cmd(wsl_dir, @$"--mount \\.\PHYSICALDRIVE{disk_number}");
                    // No error code management, as the disk will either mount or just stay mounted



                    // Gets list of partitions of non-virtualized disks

                    Console.WriteLine("Starting WSL process & getting mounted disks' partitions...");
                    Process wsl = new Process();
                    wsl.StartInfo.FileName = wsl_dir;
                    wsl.StartInfo.Arguments = "-u root blkid | grep \"$(sudo lshw -class disk | grep \"logical name\" | awk '{print $3}')\" && exit";
                    wsl.StartInfo.UseShellExecute = false;
                    wsl.StartInfo.RedirectStandardInput = true;
                    wsl.StartInfo.RedirectStandardOutput = true;
                    wsl.Start();
                    string wsl_output = wsl.StandardOutput.ReadToEnd();
                    wsl.WaitForExit();

                    // Prints out list of disk partitions
                    Console.WriteLine(wsl_output);

                    while (true) // Loop for user input
                    {

                        Console.WriteLine("Select partition [sdXY]:\n>");
                        Console.SetCursorPosition(2, Console.CursorTop - 1);
                        string part_no = Console.ReadLine() ?? "NONE";

                        Console.WriteLine($"Selected {part_no.ToLower()}, is that correct?\n>");
                        Console.SetCursorPosition(2, Console.CursorTop - 1);
                        string conf = Console.ReadLine() ?? "NONE";

                        // welcome to indentation hell

                        if (yes.Contains(conf)) // User input check
                        {

                            foreach (string line in wsl_output.Split('\n')) // Splits list of partitions into lines
                            {

                                if (line.Contains(part_no.ToLower())) // If the line contains the correct partition, continue

                                {
                                    foreach (string parameter in line.Split(' ')) // Split the correct line into parameters (A="B" C="D" etc.)
                                    {

                                        string[] param_info = parameter.Split('='); // Separates the parameters contents from the parameter itself

                                        // If the line split is indeed a parameter, make it readable by removing quotation marks
                                        if (parameter.Contains("=")) { param_contents = param_info[1].Replace("\"", ""); }


                                        else { continue; }


                                        if (param_info[0] == "PARTUUID") // If the PARTUUID parameter is found
                                        {

                                            // Mounts drive using a passed WSL command, bypassing WSL kernel modules used in wsl.exe --mount
                                            Process wsl_mount = new Process();
                                            wsl_mount.StartInfo.FileName = wsl_dir;
                                            wsl_mount.StartInfo.Arguments = $"-u root";
                                            wsl_mount.StartInfo.UseShellExecute = false;
                                            wsl_mount.StartInfo.RedirectStandardInput = true;
                                            wsl_mount.Start();
                                            StreamWriter wsl_mount_input = wsl_mount.StandardInput;

                                            // Makes the directory for the disk drive | Mounts the disk as an user | Exits the process safely
                                            wsl_mount_input.Write(@$"sudo mkdir -p /mnt/wsl/{param_contents} && sudo mount -O uid=1000,gid=1000 '{parameter}' '/mnt/wsl/{param_contents}' && exit");

                                            wsl_mount_input.Close();
                                            wsl_mount.WaitForExit();
                                            //sudo mkdir -p /mnt/abc && sudo mount -O uid=1000,gid=1000 'PARTUUID="1fdd2df9-018b-4308-b328 - bee3f3721e52"' '/mnt/abc'

                                            if (wsl_mount.ExitCode == 0)
                                            {
                                                Console.WriteLine("Disk mounted to WSL succesfully!");
                                            }
                                            else
                                            {
                                                Console.WriteLine("Something went wrong with mounting, stopping program!");
                                                run_cmd("PAUSE");
                                                Environment.Exit(1);
                                            }
                                        }

                                    }

                                }
                            }
                            break;
                        }

                        // Temporary fix
                        Console.WriteLine("Running SUBST on disk, please do not close this window until you want the drive letter to be discarded!");
                        Console.WriteLine("Note that this is merely a temporary fix until a fix is developed.");
                        Tuple<string, int> subst_cmd = run_cmd(@$"subst {drive_letter}: \\WSL$\Ubuntu\mnt\wsl\{param_contents}");

                        if (subst_cmd.Item2 == 0)
                        {
                            Console.WriteLine($"SUBST Succesful! \n Partition succesfully mounted on {drive_letter}:\\");
                            run_cmd("PAUSE");
                            Console.WriteLine(@$"Unpaused shell, SUBST will not work anymore. The disk will remain mounted in \\WSL$\<distro>\mnt\wsl\{param_contents} indefinitely.");
                            Environment.Exit(0);
                        }
                        else { Console.WriteLine(@"SUBST failed! (Is \\WSL$\ available?) "); run_cmd("PAUSE"); Environment.Exit(1); }

                    }





                }
                // TODO: Unmount mode for new version
                // Current version borked, pending update

                // UNMOUNT MODE
                //else if (unmount.Contains(mode.ToLower()))
                else if (!true)
                {

                    // SUBST UNMOUNT
                    // Gets the list of network drives
                    while (true)
                    {

                        subst_output = run_cmd("subst").Item1 ?? "";
                        Console.WriteLine($"Current substs:\n{subst_output}");
                        Console.WriteLine("Select SUBST letter to unmount [<empty>/q/quit to cancel]:\n>");
                        Console.SetCursorPosition(2, Console.CursorTop - 1);
                        string select = Console.ReadLine() ?? "NULL";

                        foreach (var line in subst_output.Split('\n')) // Splits the 'subst' command into lines
                        {
                            if (quit.Contains(select.ToLower()))
                            { Console.WriteLine("Quitting!"); Environment.Exit(0); }

                            else if (line.Split(' ').Contains($@"{select.ToUpper()}:\:")) // If the line contains the desired unmount letter, proceeds to unmount
                            {
                                Tuple<string, int> cmd = run_cmd($"subst {select}: /d"); // Unmount command

                                if (cmd.Item2 == 0)
                                {
                                    Console.WriteLine($"Succesfully removed the {select.ToUpper()}:\\ subst!");

                                    // Getting PHYSICALDRIVE number, as it doesn't exist within the UNMOUNT context
                                    foreach (var term in line.Split(' '))
                                    {
                                        if (term.Contains(@"\Ubuntu\mnt\wsl\"))
                                        { disk_label = term.Substring(term.IndexOf("PHYSICALDRIVE"), 14); }
                                    }
                                    break;
                                }

                                else { Console.WriteLine("Something done broke with SUBST! Command output:\n" + cmd.Item1); run_cmd("PAUSE"); Environment.Exit(0); }
                            }
                            else { Console.WriteLine("Letter not in use or invalid letter inputted!"); }

                        }
                        break;
                    }


                    // WSL UNMOUNT
                    // Unmounts the disk in WSL, doesn't use run_cmd because of elevation

                    Console.WriteLine("Unmounting disk from WSL (This requires administrative privileges)...");
                    int proc = run_elevated_cmd("C:\\Windows\\System32\\wsl.exe", $"--unmount \\\\.\\{disk_label}");

                    if (proc != 0)
                    {
                        Console.WriteLine(@"Disk unmounted from WSL UNSUCCESFULLY! (Is it mounted? Check `\\WSL$\<distro>\mnt\wsl` to see if a disk folder is present)");
                        run_cmd("PAUSE");
                        Environment.Exit(1);
                    }
                    else if (proc == 0)
                    {
                        Console.WriteLine("DIsk unmounted from WSL succesfully");
                        run_cmd("PAUSE");
                        Environment.Exit(0);
                    }
                }

                else { Console.Write("Incorrect input!\n"); }
            }
        }
    }
}
