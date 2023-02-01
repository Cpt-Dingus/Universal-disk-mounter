﻿using System.Diagnostics;

/*
 * Made by Cpt-Dingus
 * v0.3 - 01/02/2023
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



        static void Main(string[] args)
        {

            // -- Vars -- 

            string disk_number,
                   disk_label = "NULL",
                   drive_letter = "",
                   disk_list,
                   subst_output = "";



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
                    // Disk labels & \\.\PHYSICALDRIVE path
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

                    {
                        // Prints out the partitions with byte sizes parsed
                        Console.WriteLine(run_cmd("(Get-Partition -DiskNumber " + disk_number + " | Where-Object {$_.Number -ne \\\"0\\\"} | Select-Object PartitionNumber,@{n='Size';e={$size= $_.Size; if($size -ge 1TB){[string]::Format(\\\"{0:N2} TB\\\", $size/1TB)}elseif($size -ge 1GB){[string]::Format(\\\"{0:N2} GB\\\", $size/1GB)}elseif($size -ge 1MB){[string]::Format(\\\"{0:N2} MB\\\", $size/1MB)}elseif($size -ge 1KB){[string]::Format(\\\"{0:N2} KB\\\", $size/1KB)}else{[string]::Format(\\\"{0:N2} B\\\", $size)}}},Type)").Item1);
                        Console.WriteLine("Select partition number\n>");
                        Console.SetCursorPosition(2, Console.CursorTop - 1);
                        string part_number = Console.ReadLine() ?? "NULL";

                        // Starts up wsl with the exit command passed regardless of whether it was already started or not
                        Console.WriteLine("Starting WSL...");
                        if (run_cmd("wsl exit").Item2 == 0) { Console.WriteLine("WSL Started"); };

                        // Mounts the disk in WSL, doesn't use run_cmd because of elevation
                        Console.WriteLine("Mounting drive (Requires administrator privileges)...");
                        Process proc = new Process();
                        proc.StartInfo.FileName = "C:\\Windows\\System32\\wsl.exe";
                        proc.StartInfo.Arguments = $"--mount \\\\.\\PHYSICALDRIVE{disk_number} --partition {part_number}";
                        proc.StartInfo.Verb = "runas";
                        proc.StartInfo.UseShellExecute = true;
                        proc.Start();
                        proc.WaitForExit();

                        if (proc.ExitCode == 0) { Console.WriteLine("Succesfully mounted to WSL"); }
                        else { Console.WriteLine($@"WSL Mount failed! (Check if the drive isn't already mounted in \\WSL$\<distro>\mnt\wsl\)"); run_cmd("PAUSE"); Environment.Exit(1); }
                        //TODO: Check if drive is already mounted instead of 0x1

                        // SUBSTs the localhosted WSL folder to an user selected drive letter
                        Console.WriteLine("Running SUBST on \\\\WSL$\\..");
                        if (run_cmd($"subst {drive_letter}: \\\\WSL$\\Ubuntu\\mnt\\wsl\\PHYSICALDRIVE{disk_number}p{part_number}").Item2 == 0)
                        {
                            Console.WriteLine($"SUBST Succesful! \n Partition succesfully mounted on {drive_letter}:\\");
                            Console.WriteLine("TEMPORARY: SUBST will only remain mounted as long as this window remains open, please don't close it. Fix is in development."); run_cmd("PAUSE"); break;
                        }

                        else { Console.WriteLine(@"SUBST failed! (Is \\WSL$\ available?) "); run_cmd("PAUSE"); Environment.Exit(1); }

                    }
                }



                // UNMOUNT MODE
                else if (unmount.Contains(mode.ToLower()))
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
                    Process proc = new Process();
                    proc.StartInfo.FileName = @"wsl.exe";
                    proc.StartInfo.Arguments = $"--unmount \\\\.\\{disk_label}";
                    proc.StartInfo.Verb = "runas";
                    proc.StartInfo.UseShellExecute = true;
                    proc.Start();
                    proc.WaitForExit();

                    if (proc.ExitCode != 0)
                    {
                        Console.WriteLine(@"Disk unmounted from WSL UNSUCCESFULLY! (Is it mounted? Check `\\WSL$\<distro>\mnt\wsl` to see if a disk folder is present)");
                        run_cmd("PAUSE");
                        Environment.Exit(1);
                    }
                    else if (proc.ExitCode == 0)
                    {
                        Console.WriteLine("DIsk unmounted from WSL succesfully");
                        run_cmd("PAUSE");
                        Environment.Exit(0);
                    }
                }

                else { Console.Write("Incorrect input!"); }
            }
        }
    }
}
