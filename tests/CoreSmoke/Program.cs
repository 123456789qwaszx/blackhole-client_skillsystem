using System;
using BlackHole.Skills.Tests;

internal static class Program
{
    private static int Main()
    {
        int passed = 0, failed = 0;
        foreach (var contract in SkillContracts.All())
        {
            try { contract.Run(); Console.WriteLine("PASS " + contract.Name); passed++; }
            catch (Exception error) { Console.Error.WriteLine("FAIL " + contract.Name + "\n" + error); failed++; }
        }

        Console.WriteLine($"{passed} passed, {failed} failed");
        return failed == 0 ? 0 : 1;
    }
}
