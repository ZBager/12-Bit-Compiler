using System.Reflection;
using System.Globalization;

namespace Compiler;

public class CpuCompiler
{
    private static int _pcLine;
    private static Dictionary<string, int> _labelPointers = new Dictionary<string, int>();
    private static string[] LoadProgram(string path)
    {
        string programPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);
        if (File.Exists(programPath))
            return File.ReadAllLines(programPath);
        Console.WriteLine("File does not exist"); 
        Environment.Exit(10);
        return null;
    }
    private static void CheckFlagStatus()
    { 
        Console.WriteLine("E10");
        Console.WriteLine("000");
        _pcLine += 2;
    } 
    static int Main()
    {
        foreach (string line in LoadProgram(@"../../../Data/Program.txt"))
            Parser(line);
        return 0;
    }

    private static void Parser (string line)
    {
        if (char.IsWhiteSpace(line, 0))
        {
            string[] parsedLine = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parsedLine.Length > 1)
                parsedLine[1] = parsedLine[1].TrimEnd(',');
            if (parsedLine.Length == 4)
                parsedLine[2] = parsedLine[2].TrimEnd(',');

            switch (parsedLine.Length)
            {
                case 1:
                    dec_command_1A(parsedLine[0]);
                    break;
                case 2:
                    dec_command_2A(parsedLine[0], parsedLine[1]);
                    break;
                case 3:
                    dec_command_3A(parsedLine[0], parsedLine[1], parsedLine[2]);
                    break;
                case 4:
                    dec_command_4A(parsedLine[0], parsedLine[1], parsedLine[2], parsedLine[3]);
                    break;
                default:
                    Console.WriteLine("Error on line " + _pcLine + ", wrong amount of argument.");
                    Environment.Exit(10);
                    break;
            }
            return;
        }
        Labeler(line, _pcLine);
    }

    private static void Labeler (string label, int line)
    {
        label = label.TrimEnd(':');
        try
        {
            _labelPointers.Add(label, line);
        }
        catch (ArgumentException)
        {
            Console.WriteLine("Label \"" + label + "\" is already declared on line " + _labelPointers[label] + ".");
            Environment.Exit(10);
        }
    }

    private static void dec_command_4A(string command, string argA, string argB, string label)
    { 
        switch (command)
        {
            case "JE":
                CheckFlagStatus();
                Parser(" MOVE 4, REG[13]"); 
                Parser(" CMP " + argA + ", " + argB);
                Parser(" CMOVE " + _labelPointers[label] + ", REG[15]");
                break;
            case "JLE" or "JEL":
                CheckFlagStatus();
                Parser(" MOVE 5, REG[13]");
                Parser(" CMP " + argA + ", " + argB);
                Parser(" CMOVE " + _labelPointers[label] + ", REG[15]");
                break;
            case "JG":
                CheckFlagStatus();
                Parser(" MOVE 2, REG[13]");
                Parser(" CMP " + argA + ", " + argB);
                Parser(" CMOVE " + _labelPointers[label] + ", REG[15]");
                break;
            case "JL":
                CheckFlagStatus();
                Parser(" MOVE 1, REG[13]");
                Parser(" CMP " + argA + ", " + argB);
                Parser(" CMOVE " + _labelPointers[label] + ", REG[15]");
                break;
            case "JO":
                Parser(" MOVE 8, REG[13]");
                Parser(" CMOVE " + _labelPointers[label] + ", REG[15]");
                break;
        }
    }
    private static void dec_command_3A (string command, string argA, string argB)
    {
        switch (command)
        {
            // Arithmetic
            case "ADD":
                dec_helper_3A(argA, argB, "1", "80");
                break;
            case "SUB":
                dec_helper_3A(argA, argB, "2", "90");
                break;
            case "RSUB":
                dec_helper_3A(argA, argB, "3", "A0");
                break;
            // Logic
            case "AND":
                dec_helper_3A(argA, argB, "4", "B0");
                break;
            case "OR":
                dec_helper_3A(argA, argB, "5", "C0");
                break;
            case "XOR":
                dec_helper_3A(argA, argB, "6", "D0");
                break;
            // Conditional
            case "CMP":
                dec_helper_3A(argA, argB, "B", "E0");
                break;
            // Other
            case "MOVE":
                dec_helper_3A(argA, argB, "D", "10");
                break;
            case "CMOVE":
                dec_helper_3A(argA, argB, "C", "20");
                break;
        }
    }

    private static void dec_helper_3A(string argA, string argB, string opcR, string opcI)
    {
        if (isREG(argA))
        {
            Console.WriteLine(getREGHEX(getREG(argB)) + getREGHEX(getREG(argA)) + opcR);
            _pcLine += 1;
        } else if (isVAL(argA))
        {
            Console.WriteLine(getREGHEX(getREG(argB)) + opcI);
            Console.WriteLine(getVALHEX(GetVal_HexToInt(argA)));
            _pcLine += 2;
        }
    }
        
    private static void dec_command_2A (string command, string argB)
    {
        switch (command)
        {
            case "INC":
                Console.WriteLine(getREGHEX(getREG(argB)) + "30");
                _pcLine += 1;
                break;
            case "DEC":
                Console.WriteLine(getREGHEX(getREG(argB)) + "40");
                _pcLine += 1;
                break;
        }
    }
    private static void dec_command_1A (string command)
    {
        switch (command)
        {
            case "STOP":
                Console.WriteLine("000");
                _pcLine += 1;
                break;
            case "CSTOP":
                Console.WriteLine("001");
                _pcLine += 1;
                break;
        }
    }

    private static int GetVal_HexToInt(string arg)
    {
        return Int32.Parse(arg, NumberStyles.Integer);
    }
    private static int getREG(string arg)
    {
        return Int32.Parse(arg.Substring(4, arg.Length - 5), NumberStyles.Integer);
    }
    private static string getREGHEX(int arg)
    {
        return arg.ToString("X");
    }
    private static string getVALHEX(int arg)
    {
        return arg.ToString("X3");
    }
    private static bool isVAL (string arg)
    {
        try
        {
            if (UInt32.Parse(arg, NumberStyles.Integer) <= 4095)
                return true;
            return false;
        }
        catch (Exception e)
        {
            return false;
        }
    }
    private static bool isREG (string arg)
    {
        try
        {
            if (String.Compare(arg.Substring(0, 4), "REG[") == 0 &&
                UInt32.Parse(arg.Substring(4, arg.Length - 5), NumberStyles.Integer) <= 15 &&
                String.Compare(arg.Substring(arg.Length-1, 1), "]") == 0)
                return true;
            return false;
        }
        catch (Exception e)
        {
            return false;
        }
    }
}