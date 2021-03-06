﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CodeWarsImporter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("CodeWarsImporter.exe <Username> <Password>");
                return;
            }

            string user = args[0];
            string pass = args[1];
            string token = null;

            if (args.Length >= 3)
            {
                token = args[2];
            }

            var cw = new CodeWarsAPI(user, pass, token);

            try
            {
                cw.Login();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred. Your password might be invalid.");
                Console.WriteLine();
                Console.WriteLine(ex);
                return;
            }

            var sw = new StreamWriter("Error.log");

            int batchNumber = 0;
            int total = 0;
            int success = 0;
            IEnumerable<string> katas;
            while ((katas = cw.FetchKatas(batchNumber)).Count() > 0)
            {
                foreach (var kata in katas)
                {
                    string slug = Regex.Match(kata, @"/kata/(?<Slug>[^/]*?)/").Groups["Slug"].Value;

                    ++total;
                    try
                    {
                        Console.WriteLine($"#{total} Reading {kata}");

                        var solution = cw.FetchSolution(kata);

                        Directory.CreateDirectory(solution.Kyu);

                        Console.WriteLine($"Writing <{solution.Kyu}> {slug}");
                        File.WriteAllText(Path.Combine(solution.Kyu, $"{slug}.js")
                            , $"// http://www.codewars.com/kata/{slug}\n\n"
                                + String.Join("\n\n// alternative solution\n", solution.Solutions));
                        ++success;
                    }
                    catch (Exception ex)
                    {
                        sw.WriteLine(kata + " -> " + ex);
                    }
                }
                ++batchNumber;
            }

            sw.Close();

            Console.WriteLine($"All done! Total: {success}/{total}");
            Console.ReadKey(true);
        }
    }
}
