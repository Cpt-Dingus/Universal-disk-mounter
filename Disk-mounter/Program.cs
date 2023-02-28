using System.Diagnostics;


/*
 * Made by Cpt-Dingus
 * v1.0 - 28/02/2023
 * CLI VERSION
 */




namespace disk_mounter
{
    class Program
    {

        // -- Functions --
        public static Tuple<string, int> run_cmd(string cmd, string file = "powershell.exe")
        {
            Process process = new();
            process.StartInfo.FileName = file;
            process.StartInfo.Arguments = $"-command \"{cmd}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();
            // Fix for funky shit that stdout outputs to string
            string output = process.StandardOutput.ReadToEnd().Replace("\0", "").Replace("\r", "");
            process.WaitForExit();

            return Tuple.Create(output, process.ExitCode);
        }


        public static int run_elevated_cmd(string cmd, string args)
        {
            // Doesn't have STDOUT because csharp is dumb and can't have UsesShellExecute set
            // to true with redirected STDOUT, but needs it to be true for it to run elevated

            Process proc = new();
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

            // --> Vars <-- 

            string disk_label = "NULL",
                   drive_letter = "",
                   param_contents = "",
                   disk_list,
                   disk_number, // isn't an int because too lazy to figure out concat
                   distro,
                   part_name;

            const string wsl_dir = @"C:\Windows\System32\wsl.exe";

            string[] yes = { "y", "yes" },
                     alphabet = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" },
                     distro_list = Array.Empty<string>();

            bool dtc = true,
                 subst_passed = false,
                 wsl_mounted = false;

            Dictionary<string, string> disk_info = new();




            // --> Base checks <--

            Console.WriteLine("Running base checks...");

            // Checks if wsl.exe exists
            if (!File.Exists(wsl_dir))
            {
                Console.WriteLine(@"WSL executable not found! Please make sure it is installed and enabled at C:\Windows\System32\wsl.exe");
                run_cmd("PAUSE"); Environment.Exit(1);
            }

            // Checks if program is being run as admin
            else if (run_cmd("net session").Item2 == 0)
            {
                Console.WriteLine("Detected elevated permissions, this program has to be run from an unelevated shell!");
                run_cmd("PAUSE"); Environment.Exit(1);
            }

            // Clears base checks from command line history
            run_cmd("cls");








            // --> Distro selection <--

            foreach (string line in run_cmd("wsl --list --all").Item1.Split("\n")) // Gets each distro using WSL
            {
                // Excepts the starting line ("WSL Distributions:"), excludes docker
                if (dtc || line.Contains("docker", StringComparison.CurrentCulture)) { dtc = false; continue; }

                Array.Resize(ref distro_list, distro_list.Length + 1);
                distro_list[distro_list.Length - 1] = line;
            }

            // Skips last blank line
            Array.Resize(ref distro_list, distro_list.Length - 1);

            dtc = true; // Manky solution to make sure an iteration is skipped but whatever, it works


            // Prints distros out to the console
            Console.WriteLine("Available distributions to use:");
            for (int i = 0; i != distro_list.Length; i++) { Console.WriteLine($"{i + 1}) {distro_list[i]}"); }
            

            while (true) // Loop for user input
            {
                // Stops the script if there aren't any 
                if (distro_list.Length == 0) { Console.WriteLine("No valid distributions found! Install a distrubution (Excluding docker) to proceed."); Environment.Exit(1); }

                // Skips selection if there is only one available distribution
                else if (distro_list.Length == 1) { Console.WriteLine("Only one valid distribution detected, auto-selecting...\n"); distro = distro_list[0].Replace(" (Default)", ""); break; }



                Console.WriteLine("Please select the distribution to use for the backend:\n>");
                Console.SetCursorPosition(2, Console.CursorTop - 1);
                string distro_number = Console.ReadLine() ?? "NULL";

                // Checks if number is valid
                // Checks -> Not an int, Value lower than 1, Value higher than the length of the distro list
                if (int.TryParse(distro_number, out _) is false || Int32.Parse(distro_number) < 1 || Int32.Parse(distro_number) > distro_list.Length) { Console.WriteLine("Invalid input!"); continue; }

                // Assigns distro
                distro = distro_list[Int32.Parse(distro_number) - 1];

                break;
            }




            // --> Main <--

            // - MOUNT -

            // Disk labels & \\.\PHYSICALDRIVE path
            Console.WriteLine("Getting disks...");

            // Later used for disk label confirmation
            disk_list = run_cmd("GET-CimInstance -query \\\"SELECT DeviceID,Caption from Win32_DiskDrive\\\" | Select-Object DeviceID,Caption | Sort-Object DeviceID").Item1;
            Console.WriteLine(disk_list);



            while (true) // Input loop
            {
                Console.Write("Select disk number:\n>");
                Console.SetCursorPosition(2, Console.CursorTop);
                disk_number = Console.ReadLine() ?? "NULL"; // Later used for SUBST command


                // Int check for user input
                // Checks -> Not an int, Null number, Value lower than 0, Value higher than the PHYSICALDRIVE output with subtracted extra lines)
                if (int.TryParse(disk_number, out _) is false || disk_number == "NULL" || Int32.Parse(disk_number) < 0 || Int32.Parse(disk_number) > disk_list.Split("\n").Length - 5)
                { Console.WriteLine("Invalid input!"); continue; }




                // Splits the disk list into newlines, if it finds the correct disk pulls the label
                foreach (string line in disk_list.Split('\n'))
                {
                    // Splits line into list by spaces
                    string[] line_split = line.TrimEnd().Split(' ');

                    // If the correct disk is found, gets the disk label for confirmation
                    if (line_split[0] == $"\\\\.\\PHYSICALDRIVE{disk_number}") { disk_label = string.Join(" ", line_split.Skip(1).ToArray()) ?? "NULL"; }
                }

                // Null check
                if (disk_label == "NULL") { Console.WriteLine("Disk label was read as NULL"); Environment.Exit(1); }


                // Disk label confirmation

                Console.WriteLine($"You selected {disk_label}, is that correct? [Y/N]\n>");
                Console.SetCursorPosition(2, Console.CursorTop - 1);
                string input = Console.ReadLine() ?? "NULL";

                if (yes.Contains(input.ToLower())) { break; }

            }



        Letter_select:

            Console.WriteLine("Please select desired drive letter:\n>");
            Console.SetCursorPosition(2, Console.CursorTop - 1);
            drive_letter = Console.ReadLine() ?? "C"; // C will mark as invalid

            // Checks -> if the letter doesn't contain an alphabetical character, if there was more than one char inputted or if is already used
            if (!alphabet.Contains(drive_letter.ToLower()) || drive_letter.Length > 1 || Directory.Exists($"{drive_letter}:\\"))
            { Console.WriteLine("Letter already in use or invalid letter inputted!"); goto Letter_select; }


            // Loops through each line of "subst" to see if one already exists for specified drive letter
            foreach (var line in run_cmd("subst").Item1.Split('\n')) // Splits 'subst' into lines
            {
                if (line.Split(' ').Contains($@"{drive_letter.ToUpper()}:\:")) { Console.WriteLine($"{drive_letter} already SUBST-ed!"); goto Letter_select; }
            }

            Console.WriteLine($@"Selected {drive_letter.ToUpper()}:\");





            // WSL Mount

            Console.WriteLine("Mounting Disk to WSL (This requires administrative privileges)...");
            run_elevated_cmd(wsl_dir, @$"--mount \\.\PHYSICALDRIVE{disk_number} --bare");
            // No error code management, as the disk will either mount or just stay mounted
            // Other errors aren't possible, the syntax is set and the dir has already been tested.
            Console.WriteLine("Disk mounted succesfully!");



            // Gets list of partitions of non-virtualized disks

            Console.WriteLine("Starting WSL process & getting mounted volumes...\n");
            Process wsl = new();
            wsl.StartInfo.FileName = wsl_dir;
            wsl.StartInfo.Arguments = "-u root blkid | grep \"$(sudo lshw -class disk | grep \"logical name\" | awk '{print $3}')\"";
            wsl.StartInfo.UseShellExecute = false;
            wsl.StartInfo.RedirectStandardInput = true;
            wsl.StartInfo.RedirectStandardOutput = true;
            wsl.Start();
            string wsl_output = wsl.StandardOutput.ReadToEnd();
            wsl.WaitForExit();



            // Prints out list of disk partitions
            Console.WriteLine(wsl_output);



            while (true) // Input loop
            { 


                Console.WriteLine("Select partition [sdXY]:\n>");
                Console.SetCursorPosition(2, Console.CursorTop - 1);
                part_name = Console.ReadLine() ?? "NONE";

                Console.WriteLine($"Selected {part_name.ToLower()}, is that correct? [Y/N]\n>");
                Console.SetCursorPosition(2, Console.CursorTop - 1);
                string conf = Console.ReadLine() ?? "NONE";


                // User input check
                if (yes.Contains(conf)) { break; }
            }



            // A mess, but it works!

            foreach (var line in wsl_output.Split('\n')) // Splits list of partitions into lines
            {
                // If the line doesn't contain the correct partition, skip to the next iteration
                if (!line.Contains(part_name.ToLower())) { continue; }


                foreach (string parameter in line.Split(' ')) // Split the correct line into parameters (A="B" C="D" etc.)
                {

                    string[] param_list = parameter.Split('='); // Separates the parameters contents from the parameter itself

                    
                    // If the value isn't a parameter, skip to the next iteration
                    if (!parameter.Contains("=")) { continue; }



                    if (param_list[0] == "PARTUUID") // If the PARTUUID parameter is found
                    {
                        param_contents = param_list[1].Replace("\"", "");

                        // Mounts drive using a passed WSL command, bypassing WSL kernel modules used in `wsl.exe --mount`
                        Process wsl_mount = new();
                        wsl_mount.StartInfo.FileName = wsl_dir;
                        // Makes the directory for the disk drive | Mounts the disk as an user | Exits the process safely
                        wsl_mount.StartInfo.Arguments = $"-u root sudo mkdir -p /mnt/wsl/{param_contents} && sudo mount -O uid=1000,gid=1000 '{parameter}' '/mnt/wsl/{param_contents}' && exit";
                        wsl_mount.StartInfo.UseShellExecute = false;
                        wsl_mount.StartInfo.RedirectStandardInput = true;

                        wsl_mount.Start();
                        wsl_mount.WaitForExit();

                        wsl_mounted = true;

                        if (wsl_mount.ExitCode == 0) { Console.WriteLine("Disk mounted to WSL succesfully!"); }
                        else
                        {
                            Console.WriteLine("Something went wrong with mounting, stopping program!");
                            run_cmd("PAUSE");
                            goto Unmount; // Cleans up mounts
                        }
                    }
                }
                break;  // This only gets reached once the mounting is done, breaks out of the initial foreach loop.
            }




            // Sub-optimal solution, but it works (The console window has to stay open for the SUBST to remain)
            // This will get addressed in the GUI version.

            Console.WriteLine("Running SUBST on disk...");

            Tuple<string, int> subst_cmd = run_cmd($@"subst {drive_letter}: \\WSL$\{distro}\mnt\wsl\{param_contents}");

            if (subst_cmd.Item2 == 0)
            {
                subst_passed = true;
                Console.WriteLine($"SUBST Succesful! \n Partition succesfully mounted on {drive_letter.ToUpper()}:\\");
                Console.WriteLine("Shell paused, please do not close this window until you want the drive letter to be discarded! Pressing enter will automatically unmount.");
                run_cmd("PAUSE");
                Console.WriteLine(@$"Unpaused shell, unmounting");
            }
            else { Console.WriteLine("SUBST failed! (Is \\\\WSL$\\ available?) Command output: \n"); run_cmd("PAUSE"); goto Unmount; }




        // - UNMOUNT -
       
        Unmount:

            Console.WriteLine("\n\n--- Cleaning up! ---\n");

            // Un-SUBST, performed if it happened prior to any errors
            if (subst_passed)
            {
                Console.WriteLine("Un-SUBST-ing volume...");
                Tuple<string, int> subst_result = run_cmd($"subst {drive_letter}: /d");

                if (subst_result.Item2 == 1)
                {
                    Console.WriteLine($"Failed to un-SUBST volume! Command output: {subst_result.Item1}");
                    run_cmd("PAUSE");
                    Environment.Exit(1);
                }
                Console.WriteLine("Succesfully Un-SUBST-ed!\n");
            }


            // WSL Partition unmount, performed if it happened prior to any errors
            if (wsl_mounted)
            {
                Console.WriteLine("Unmounting partition in WSL...");
                Tuple<string, int> wsl_part_unmount = run_cmd($"{wsl_dir} -u root umount /mnt/wsl/{param_contents}");

                if (wsl_part_unmount.Item2 == 1)
                {
                    Console.WriteLine($"Failed to unmount partition in WSL!");
                    run_cmd("PAUSE");
                    Environment.Exit(1);
                }
                Console.WriteLine("Partition succesfully unmounted!\n");
            }

            

            // WSL Disk unmount, happens regardless as any errors earlier than it exit the process (as no changes were made)
            Console.WriteLine("Unmounting disk from WSL (This requires administrative privileges)...");
    
            if (run_elevated_cmd("C:\\Windows\\System32\\wsl.exe", $"--unmount \\\\.\\PHYSICALDRIVE{disk_number}") == 0)
            {
                Console.WriteLine("Disk unmounted from WSL succesfully!\n");
                run_cmd("PAUSE");
                Environment.Exit(0);
            }

            else // Any exit code besides 0 is a failure
            {
                Console.WriteLine(@"Disk unmounted from WSL UNSUCCESFULLY! (Is it mounted? Check `\\WSL$\<distro>\mnt\wsl` to see if a disk folder is present");
                run_cmd("PAUSE");
                Environment.Exit(1);
            }
        }

    }
}
