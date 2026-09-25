using System;
using System.IO;
using System.Text.Json;
using BlackHole.Skills;
using BlackHole.Skills.TestPack;
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
            var progress = new SkillProgress(result.Catalog);
            if (progress.Purchase("breaker-2") != UpgradeResult.Purchased)
                throw new Exception("샘플 NodeId를 구매할 수 없다.");
            var battle = new SkillBattle(progress, 1, 9, new FixedRandom(0)) { Aim = new Point2(0, 0) };
            EnemyTarget enemy = battle.AddEnemy(new Point2(0, 0), 10);
            battle.Advance(0);
            if (enemy.Health != 6) throw new Exception("샘플 Lv2의 Damage가 적용되지 않았다.");
            battle.End();
            Console.WriteLine("PASS Data.SampleGameplayJson"); passed++;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL Data.SampleGameplayJson\n" + error); failed++; }

        Console.WriteLine($"{passed} passed, {failed} failed");
        return failed == 0 ? 0 : 1;
    }
}
