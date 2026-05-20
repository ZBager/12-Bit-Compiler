using System.Reflection;
using System.Globalization;

namespace Compiler;

public class CpuCompiler
{
    // Lists, Pointers and Dictionaries
    private static int _pcLine;
    private static Dictionary<string, int> _labelPointers = new Dictionary<string, int>();
    private static Dictionary<string, int[]> _labelUsages = new Dictionary<string, int[]>();
    private static string[] _program = {"START"};
    // Loading Program
    private static string[] LoadProgram(string path)
    {
        string programPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);
        if (File.Exists(programPath))
            return File.ReadAllLines(programPath);
        Console.WriteLine("File does not exist"); 
        Environment.Exit(10);
        return null;
    }
    private static void ClearFlags()
    {
        _program = _program.Append("E10").ToArray();
        _program = _program.Append("000").ToArray();
        _pcLine += 2;
    } 
    static int Main()
    {
        foreach (string line in LoadProgram(@"../../../Data/Program.txt"))
        {
            Parser(line);
        }

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
                ClearFlags();
                Parser(" MOVE 4, REG[13]"); 
                Parser(" CMP " + argA + ", " + argB);
                jump_helper(label);
                break;
            case "JLE" or "JEL":
                ClearFlags();
                Parser(" MOVE 5, REG[13]");
                Parser(" CMP " + argA + ", " + argB);
                jump_helper(label);
                break;
            case "JG":
                ClearFlags();
                Parser(" MOVE 2, REG[13]");
                Parser(" CMP " + argA + ", " + argB);
                jump_helper(label);
                break;
            case "JL":
                ClearFlags();
                Parser(" MOVE 1, REG[13]");
                Parser(" CMP " + argA + ", " + argB);
                jump_helper(label);
                break;
            case "JO":
                Parser(" MOVE 8, REG[13]");
                jump_helper(label);
                break;
        }
    }

    private static void jump_helper(string label)
    {
        _program = _program.Append("F20").ToArray();
        _pcLine += 1;
        _program = _program.Append(label).ToArray();
        // _labelUsages.Add(label, _labelUsages[label].Append(_pcLine));
        _pcLine += 1;
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
            _program = _program.Append(getREGHEX(getREG(argB)) + getREGHEX(getREG(argA)) + opcR).ToArray();
            _pcLine += 1;
        } else if (isVAL(argA))
        {
            _program = _program.Append(getREGHEX(getREG(argB)) + opcI).ToArray();
            _program = _program.Append(getVALHEX(GetVal_HexToInt(argA))).ToArray();
            _pcLine += 2;
        }
    }
    private static void dec_command_2A (string command, string argB)
    {
        switch (command)
        {
            case "INC":
                _program = _program.Append(getREGHEX(getREG(argB)) + "30").ToArray();
                _pcLine += 1;
                break;
            case "DEC":
                _program = _program.Append(getREGHEX(getREG(argB)) + "40").ToArray();
                _pcLine += 1;
                break;
        }
    }
    private static void dec_command_1A (string command)
    {
        switch (command)
        {
            case "STOP":
                _program = _program.Append("000").ToArray();
                _pcLine += 1;
                break;
            case "CSTOP":
                _program = _program.Append("001").ToArray();
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