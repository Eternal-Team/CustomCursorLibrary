using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace CustomCursor;

public class CustomCursor : Mod
{
	public override void Load()
	{
		IL_Main.DrawInterface_36_Cursor += DrawCursor;

		/*On_Main.DrawInventory += (orig, self) =>
		{
			orig(self);
			SetCursor("Terraria/Images/UI/Cursor_7");
		};*/
	}
	
	private const int CustomCursorOverride = 1000;
	private static Asset<Texture2D> CursorTexture;
	private static Vector2 CursorOffset;
	private static bool Pulse;

	public static void SetCursor(string texture, Vector2? offset = null, bool pulse = true)
	{
		Main.cursorOverride = CustomCursorOverride;
		CursorTexture = ModContent.Request<Texture2D>(texture);
		CursorOffset = offset ?? Vector2.Zero;
		Pulse = pulse;
	}

	private static void DrawCursor(ILContext il)
	{
		ILCursor cursor = new ILCursor(il);
		ILLabel label = cursor.DefineLabel();

		FieldInfo fieldCursorOverride = typeof(Main).GetField("cursorOverride", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		if (fieldCursorOverride is null) throw new NullReferenceException("Failed to obtain cursorOverride field");
		
		if (cursor.TryGotoNext(i => i.MatchLdsfld(fieldCursorOverride)))
		{
			cursor.Emit(OpCodes.Ldsfld, fieldCursorOverride);
			cursor.Emit(OpCodes.Ldc_I4, CustomCursorOverride);
			cursor.Emit(OpCodes.Ceq);
			cursor.Emit(OpCodes.Brfalse, label);

			cursor.Emit(OpCodes.Ldloc, 4);
			cursor.Emit(OpCodes.Ldloc, 6);

			cursor.EmitDelegate<Action<float, float>>((rotation, scale) =>
			{
				if (CursorTexture is null) return;

				float texScale = Math.Min(20f / CursorTexture.Value.Width, 20f / CursorTexture.Value.Height);
				float s = Pulse ? Main.cursorScale * texScale : texScale;
				Main.spriteBatch.Draw(CursorTexture.Value, new Vector2(Main.mouseX, Main.mouseY), null, Color.White, rotation, CursorOffset, s, SpriteEffects.None, 0f);
			});
			cursor.Emit(OpCodes.Ret);

			cursor.MarkLabel(label);
		}
	}
}