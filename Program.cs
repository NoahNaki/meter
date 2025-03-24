using KiwiAFK.DamageMeter;
using KiwiAFK.Memory;
using Microsoft.VisualBasic;

namespace DPSMeter
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var reader = new MemoryReader();
                Console.WriteLine("Combat Log service is running...");

                while (true)
                {
                    try
                    {
                        List<string> combatLog = reader.GetCombatChatLog();

                        foreach (string line in combatLog)
                        {
                            if (!string.IsNullOrEmpty(line))
                            {
                                var (actor, damage, target, skill, critical) = CombatLogParser.EvaluateCombatLogLine(line);

                                if (actor != null && damage > 0 && target != null)
                                {
                                    Console.WriteLine($"{actor} dealt {damage} damage to {target} with {skill} (Critical: {critical})");
                                }
                            }
                        }

                        await Task.Delay(1000);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in combat log processing: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                        }

                        await Task.Delay(5000);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error in Combat Log service: {ex.Message}");
            }
        }
    }
}
