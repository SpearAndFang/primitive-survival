using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace Achievements
{
    internal class AchievementsManager
    {
        static Assembly cachedAchievementsAssembly = null;
        static MethodInfo registerAchievementMethod = null;
        static MethodInfo unlockAchievementMethod = null;
        static MethodInfo isAchievementUnlockedMethod = null;

        public static void RegisterAchievement(string modId, string achievementId, string achievementGraphicCode)
        {
            if (registerAchievementMethod == null)
            {
                Assembly a = GetAchievementsAssembly();
                if (a == null) return;
                Type t = a.GetType("Achievements.AchievementsApi");
                registerAchievementMethod = t.GetMethod("RegisterAchievement");
            }
            registerAchievementMethod.Invoke(null, new object[] { modId, achievementId, achievementGraphicCode });
        }

        public static void UnlockAchievementForPlayer(string achievementId, EntityPlayer player)
        {
            if (unlockAchievementMethod == null)
            {
                Assembly a = GetAchievementsAssembly();
                if (a == null) return;
                Type t = a.GetType("Achievements.AchievementsApi");
                unlockAchievementMethod = t.GetMethod("UnlockAchievementForPlayer");
            }
            unlockAchievementMethod.Invoke(null, new object[] { achievementId, player });
        }

        public static bool IsAchievementUnlockedForPlayer(string achievementId, EntityPlayer player, bool defaultIfNotInstalled = false)
        {
            if (isAchievementUnlockedMethod == null)
            {
                Assembly a = GetAchievementsAssembly();
                if (a == null) return defaultIfNotInstalled;
                Type t = a.GetType("Achievements.AchievementsApi");
                isAchievementUnlockedMethod = t.GetMethod("IsAchievementUnlockedForPlayer");
            }
            return (bool)isAchievementUnlockedMethod.Invoke(null, new object[] { achievementId, player });
        }

        static Assembly GetAchievementsAssembly()
        {
            if (cachedAchievementsAssembly != null) return cachedAchievementsAssembly;
            AppDomain cDomain = AppDomain.CurrentDomain;
            Assembly[] assemblies = cDomain.GetAssemblies();
            foreach (Assembly a in assemblies)
            {
                if (a.GetName().Name == "natsachievements")
                {
                    cachedAchievementsAssembly = a;
                    return cachedAchievementsAssembly;
                }
            }
            return null;
        }
    }
}