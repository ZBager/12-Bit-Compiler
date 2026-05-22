using System.Reflection;
using System.Globalization;

namespace Compiler;

public class CpuCompiler
{
    // Lists, Pointers and Dictionaries
    private static int _pcLine = 0;
    private static Dictionary<string, int> _labelPointers = new Dictionary<string, int>();
    private static Dictionary<string, List<int>> _labelUsages = new Dictionary<string, List<int>>();
    private static List<string> _program = new List<string>();
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
        _program.Add("E10");
        _program.Add("000");
        _pcLine += 2;
    } 
    static int Main()
    {
        foreach (string line in LoadProgram(@"../../../Data/Program.txt"))
            Parser(line);

        foreach (var KEY in _labelUsages)
            foreach (var LINE in KEY.Value)
                _program[LINE] = getVALHEX(_labelPointers[KEY.Key]);

        foreach (var LINE in _program)
            Console.WriteLine(LINE);

        // foreach (var KEY in _labelPointers)
        // {
        //     Console.WriteLine("Key: " + KEY.Key + " Value: " + KEY.Value);
        // }
        //
        // foreach (var KEY in _labelUsages)
        // {
        //     Console.WriteLine("Key: " + KEY.Key);
        //     Console.Write("Values: ");
        //     foreach (var LINES in KEY.Value)
        //     {
        //         Console.Write(LINES + ", ");
        //     }
        //     Console.WriteLine();
        // }
        //
        // Console.WriteLine("Written " + _pcLine + " lines");
        // Console.WriteLine("Should be " + _program.Count + " lines");
        
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
            case "JGE" or "JEG":
                ClearFlags();
                Parser(" MOVE 6, REG[13]");
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
        _program.Add("F20");
        _pcLine += 1;
        _program.Add(label);
        if (!_labelUsages.ContainsKey(label))
            _labelUsages[label] = new List<int>();
        _labelUsages[label].Add(_pcLine);
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
        if (checkType(argB) == 3)
        {
            _program.Add(getREGHEX(getValue(argA)) + getREGHEX(getValue(argB)) + "E");
            _pcLine += 1;
            return;
        }
        switch (checkType(argA))
        {
            case 1:
                _program.Add(getREGHEX(getValue(argB)) + getREGHEX(getValue(argA)) + opcR);
                _pcLine += 1;
                break;
            case 2:
                _program.Add(getREGHEX(getValue(argB)) + opcI);
                _program.Add(getVALHEX(getValue(argA)));
                _pcLine += 2;
                break;
            case 3:
                _program.Add(getREGHEX(getValue(argB)) + getREGHEX(getValue(argA)) + "F");
                _pcLine += 1;
                break;
                
        }
    }
    private static void dec_command_2A (string command, string argB)
    {
        switch (command)
        {
            case "INC":
                _program.Add(getREGHEX(getValue(argB)) + "30");
                _pcLine += 1;
                break;
            case "DEC":
                _program.Add(getREGHEX(getValue(argB)) + "40");
                _pcLine += 1;
                break;
        }
    }
    private static void dec_command_1A (string command)
    {
        switch (command)
        {
            case "STOP":
                _program.Add("000");
                _pcLine += 1;
                break;
            case "CSTOP":
                _program.Add("001");
                _pcLine += 1;
                break;
        }
    }
    private static string getREGHEX(int arg)
    {
        return arg.ToString("X");
    }
    private static string getVALHEX(int arg)
    {
        return arg.ToString("X3");
    }
    private static int getValue(string arg)
    {
        switch (checkType(arg))
        {
            case 1:
                return Int32.Parse(arg.Substring(4, arg.Length - 5), NumberStyles.Integer);
            case 2:
                return Int32.Parse(arg, NumberStyles.Integer);
            case 3:
                return Int32.Parse(arg.Substring(8, arg.Length - 10), NumberStyles.Integer);
        }
        Console.WriteLine(arg);
        Environment.Exit(20);
        return 0;
    }
    private static int checkType(string arg)
    {
        if (UInt32.TryParse(arg, NumberStyles.Integer, null, out uint value2) && value2 <= 4095)
            return 2;
        if (String.Compare(arg.Substring(0, 4), "REG[") == 0 && String.Compare(arg.Substring(arg.Length-1, 1), "]") == 0)
            if (UInt32.TryParse(arg.Substring(4, arg.Length - 5), NumberStyles.Integer, null, out uint value) && value <= 15)
                return 1;
        if (String.Compare(arg.Substring(0, 8), "RAM[REG[") == 0 && String.Compare(arg.Substring(arg.Length-2, 2), "]]") == 0)
            if (UInt32.TryParse(arg.Substring(8, arg.Length - 10), NumberStyles.Integer, null, out uint value) && value <= 15)
                return 3;
        Console.WriteLine("Type error arg: " + arg);
        return 0;
    }
}