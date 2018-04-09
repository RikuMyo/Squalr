﻿namespace Squalr.Engine.Internal.Proxy32
{
    using System;

    /// <summary>
    /// Program to handle operations that are required to be run in 32 bit mode.
    /// This is needed when Squalr is running in 64 bit and editing a 32 bit application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Gets or sets the proxy program.
        /// </summary>
        private static SqualrProxy.SqualrProxy Proxy { get; set; }

        /// <summary>
        /// Main entry point.
        /// </summary>
        /// <param name="args">The args to pass to the proxy service.</param>
        public static void Main(String[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Invalid number of arguments");
            }
            else
            {
                Program.Proxy = new SqualrProxy.SqualrProxy(Int32.Parse(args[0]), args[1]);
            }

            Console.ReadLine();
        }
    }
    //// End class
}
//// End namespace