using System;
using System.IO;
using System.Text.Json;
using BlackHole.Skills;
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

        try
        {
            string path = Path.Combine("Assets", "BlackHole", "SkillSystem", "Resources", "gameplay.json");
            var data = JsonSerializer.Deserialize<GameplayData>(File.ReadAllText(path),
                new JsonSerializerOptions { IncludeFields = true });
            SkillLoadResult result = SkillCatalog.Load(data);
            if (!result.Succeeded) throw new Exception(string.Join("\n", result.Diagnostics));
            Console.WriteLine("PASS Data.SampleGameplayJson"); passed++;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL Data.SampleGameplayJson\n" + error); failed++; }

        Console.WriteLine($"{passed} passed, {failed} failed");
        return failed == 0 ? 0 : 1;
    }
}
