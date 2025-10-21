﻿using System;
using System.Diagnostics;
using System.Reflection;
namespace JP_NET.Lab1
{
    public partial class ClassA
    {
        private int _a;
        public int A { get => _a; set => _a = value; }
        public int b;
        public void MethodOfClassA()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(asm.Location);
            var x = String.Format("{0}.{1}", fvi.ProductMajorPart,
            fvi.ProductMinorPart);
            PartialMethodOfClassA(x);
        }
        partial void PartialMethodOfClassA(object x);
        partial void PartialMethodOfClassA(object x)
        {
            System.Console.WriteLine("Wydruk z" + " nowej " + "klasy A. {0}", x);
        }
    }
}