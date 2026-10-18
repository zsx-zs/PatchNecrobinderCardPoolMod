using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace PatchNecrobinderCardPoolMod.Cards
{
	[ModInitializer(nameof(Initialize))]
	public static class ModInitializer
	{
		public static void Initialize()
		{
			var harmony = new Harmony("TestNecrobinderCardPool.zyfeng");
			harmony.PatchAll();

			Log.Info("加载成功！");
		}
	}
}
