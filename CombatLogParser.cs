using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KiwiAFK.DamageMeter
{
    public class CombatLogParser
    {
        private static readonly Regex OtherPlayerExpression = new Regex(
            @"(?<target>.+?) received (?<damage>\d+(,\d+)?) (?<critical>Critical Damage|damage)( and Daze)? from (?<actor>.+?)&apos;s (?<skill>.+?)\.");

        private static readonly Regex YouExpression = new Regex(
            @"(?<skill>.+?) (?<critical>critically hit|hit) (?<target>.+?) for (?<damage>\d+(,\d+)?) damage\.");

        private static readonly Regex ReceivedExpression1 = new Regex(
            @"Received (?<damage>\d+(,\d+)?) damage from (?<actor>.+?)&apos;s (?<skill>.+?)\.");

        private static readonly Regex ReceivedExpression2 = new Regex(
            @"(?<actor>.+?)&apos;s (?<skill>.+?) inflicted (?<damage>\d+(,\d+)?) damage( and (?<debuff>.+?))?\.");

        private static readonly Regex BlockedExpression = new Regex(
            @"Blocked (?<actor>.+?)&apos;s (?<skill>.+?) but received (?<damage>\d+(,\d+)?) damage\.");

        // Track only the previous line to check for duplicates
        private string lastProcessedLine;

        /// <summary>
        /// Processes a single combat log line and returns damage entry if it's not a duplicate
        /// </summary>
        /// <param name="logLine">The combat log line to process</param>
        /// <returns>A damage entry if valid and not a duplicate of the previous line</returns>
        public (string actor, int damage, string target, string skill, bool critical)? ProcessLogLine(string logLine)
        {
            // Skip empty lines
            if (string.IsNullOrWhiteSpace(logLine))
            {
                return null;
            }

            // Skip if this is a duplicate of the last line
            if (logLine == lastProcessedLine)
            {
                return null;
            }

            // Process the line
            var result = EvaluateCombatLogLine(logLine);

            // Remember this line for duplicate checking
            lastProcessedLine = logLine;

            // If we got a valid damage entry, return it
            if (result.actor != null)
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Resets the parser state
        /// </summary>
        public void Reset()
        {
            lastProcessedLine = null;
        }

        /// <summary>
        /// Parses a single combat log line and extracts damage information
        /// </summary>
        public static (string actor, int damage, string target, string skill, bool critical) EvaluateCombatLogLine(string line)
        {
            Match otherPlayerMatch = OtherPlayerExpression.Match(line);
            if (otherPlayerMatch.Success)
            {
                string target = otherPlayerMatch.Groups["target"].Value;
                string actor = otherPlayerMatch.Groups["actor"].Value;
                int damage = int.Parse(otherPlayerMatch.Groups["damage"].Value.Replace(",", ""));
                string skill = otherPlayerMatch.Groups["skill"].Value;
                bool critical = otherPlayerMatch.Groups["critical"].Value.ToLower().Contains("critical");
                return (actor, damage, target, skill, critical);
            }

            Match youMatch = YouExpression.Match(line);
            if (youMatch.Success)
            {
                string target = youMatch.Groups["target"].Value;
                int damage = int.Parse(youMatch.Groups["damage"].Value.Replace(",", ""));
                string skill = youMatch.Groups["skill"].Value;
                bool critical = youMatch.Groups["critical"].Value.ToLower().Contains("critical");
                return ("You", damage, target, skill, critical);
            }

            Match receivedMatch1 = ReceivedExpression1.Match(line);
            if (receivedMatch1.Success)
            {
                string actor = receivedMatch1.Groups["actor"].Value;
                int damage = int.Parse(receivedMatch1.Groups["damage"].Value.Replace(",", ""));
                string skill = receivedMatch1.Groups["skill"].Value;
                return (actor, damage, "You", skill, false);
            }

            Match receivedMatch2 = ReceivedExpression2.Match(line);
            if (receivedMatch2.Success)
            {
                string actor = receivedMatch2.Groups["actor"].Value;
                int damage = int.Parse(receivedMatch2.Groups["damage"].Value.Replace(",", ""));
                string skill = receivedMatch2.Groups["skill"].Value;
                return (actor, damage, "You", skill, false);
            }

            Match blockedMatch = BlockedExpression.Match(line);
            if (blockedMatch.Success)
            {
                string actor = blockedMatch.Groups["actor"].Value;
                int damage = int.Parse(blockedMatch.Groups["damage"].Value.Replace(",", ""));
                string skill = blockedMatch.Groups["skill"].Value;
                return (actor, damage, "You", skill, false);
            }

            return (null, 0, null, null, false);
        }
    }
}
