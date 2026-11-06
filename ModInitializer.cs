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

			Log.Info("亡灵契约师卡池调整mod加载成功！作者：zyfeng");
		}
	}
}
