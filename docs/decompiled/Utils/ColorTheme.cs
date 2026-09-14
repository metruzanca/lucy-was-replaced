using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu]
public class ColorTheme : ScriptableObject
{
	[Serializable]
	public class TextInputColors : ISerializationCallbackReceiver
	{
		[NonSerialized]
		public Color32 TextColor;

		[ColorString]
		public string text;

		[NonSerialized]
		public Color32 SelectionColor;

		[ColorString]
		public string selection;

		[NonSerialized]
		public Color32 CaretColor;

		[ColorString]
		public string caret;

		public int caret_width;

		public void ApplyTo(TMP_InputField inputField)
		{
			((Graphic)(object)inputField.textComponent).color = TextColor;
			inputField.selectionColor = SelectionColor;
			inputField.caretColor = CaretColor;
			inputField.caretWidth = caret_width;
		}

		public void ApplyTo(CodeInputField inputField)
		{
			((Graphic)(object)inputField.textComponent).color = TextColor;
			inputField.selectionColor = SelectionColor;
			inputField.caretColor = CaretColor;
			inputField.caretWidth = caret_width;
		}

		public void ApplyTo(TMP_Text text)
		{
			((Graphic)(object)text).color = TextColor;
		}

		public void OnBeforeSerialize()
		{
			text = ToColorString(TextColor);
			selection = ToColorString(SelectionColor);
			caret = ToColorString(CaretColor);
		}

		public void OnAfterDeserialize()
		{
			ParseAndValidateColor(ref text, out TextColor);
			ParseAndValidateColor(ref selection, out SelectionColor);
			ParseAndValidateColor(ref caret, out CaretColor);
		}
	}

	[Serializable]
	public class SelectableColors : ISerializationCallbackReceiver
	{
		[NonSerialized]
		public Color32 NormalColor;

		[ColorString]
		public string normal;

		[NonSerialized]
		public Color32 HighlightedColor;

		[ColorString]
		public string highlighted;

		[NonSerialized]
		public Color32 PressedColor;

		[ColorString]
		public string pressed;

		[NonSerialized]
		public Color32 SelectedColor;

		[ColorString]
		public string selected;

		[NonSerialized]
		public Color32 DisabledColor;

		[ColorString]
		public string disabled;

		public float colorMultiplier = 1f;

		public float fadeDuration = 0.1f;

		public void GenerateHoveredAndPressed()
		{
			HighlightedColor = (Color)NormalColor * 1.35f;
			PressedColor = (Color)NormalColor * 1.35f * 1.35f;
		}

		public ColorBlock ToColorBlock()
		{
			ColorBlock result = default(ColorBlock);
			result.normalColor = NormalColor;
			result.highlightedColor = HighlightedColor;
			result.pressedColor = PressedColor;
			result.selectedColor = SelectedColor;
			result.disabledColor = DisabledColor;
			result.colorMultiplier = colorMultiplier;
			result.fadeDuration = fadeDuration;
			return result;
		}

		public void OnBeforeSerialize()
		{
			normal = ToColorString(NormalColor);
			highlighted = ToColorString(HighlightedColor);
			pressed = ToColorString(PressedColor);
			selected = ToColorString(SelectedColor);
			disabled = ToColorString(DisabledColor);
		}

		public void OnAfterDeserialize()
		{
			ParseAndValidateColor(ref normal, out NormalColor);
			ParseAndValidateColor(ref highlighted, out HighlightedColor);
			ParseAndValidateColor(ref pressed, out PressedColor);
			ParseAndValidateColor(ref selected, out SelectedColor);
			ParseAndValidateColor(ref disabled, out DisabledColor);
		}
	}

	[Serializable]
	public class UIColors : ISerializationCallbackReceiver
	{
		[NonSerialized]
		public Color32 BackgroundColor;

		[ColorString]
		public string background;

		public TextInputColors text;

		[NonSerialized]
		public Color32 WindowFrameColor;

		[ColorString]
		public string window_frame;

		[NonSerialized]
		public Color32 ScrollbarColor;

		[ColorString]
		public string scrollbar;

		public SelectableColors button;

		[NonSerialized]
		public Color32 ButtonTextColor;

		[ColorString]
		public string button_text;

		public SelectableColors dropdown_item;

		[NonSerialized]
		public Color32 DropdownScrollbarColor;

		[ColorString]
		public string dropdown_scrollbar;

		[NonSerialized]
		public Color32 DropdownBackgroundColor;

		[ColorString]
		public string dropdown_background;

		[NonSerialized]
		public Color32 DropdownTextColor;

		[ColorString]
		public string dropdown_text;

		public SelectableColors slider;

		[NonSerialized]
		public Color32 SliderBackgroundColor;

		[ColorString]
		public string slider_background;

		[NonSerialized]
		public Color32 OptionsMenuColor;

		[ColorString]
		public string options_menu;

		[NonSerialized]
		public Color32 OptionBackgroundColor;

		[ColorString]
		public string option_background;

		[NonSerialized]
		public Color32 OptionTextColor;

		[ColorString]
		public string option_text;

		[NonSerialized]
		public Color32 SearchMatchColor;

		[ColorString]
		public string search_match;

		[NonSerialized]
		public Color32 SearchMatchCurrentColor;

		[ColorString]
		public string search_match_current;

		[NonSerialized]
		public Color32 TooltipColor;

		[ColorString]
		public string tooltip;

		[NonSerialized]
		public Color32 WarningOverlayColor;

		[ColorString]
		public string warning_overlay;

		[NonSerialized]
		public Color32 WarningBackgroundColor;

		[ColorString]
		public string warning_background;

		[NonSerialized]
		public Color32 WarningMessageColor;

		[ColorString]
		public string warning_message;

		public void OnBeforeSerialize()
		{
			background = ToColorString(BackgroundColor);
			window_frame = ToColorString(WindowFrameColor);
			scrollbar = ToColorString(ScrollbarColor);
			button_text = ToColorString(ButtonTextColor);
			dropdown_background = ToColorString(DropdownBackgroundColor);
			dropdown_text = ToColorString(DropdownTextColor);
			dropdown_scrollbar = ToColorString(DropdownScrollbarColor);
			slider_background = ToColorString(SliderBackgroundColor);
			options_menu = ToColorString(OptionsMenuColor);
			option_background = ToColorString(OptionBackgroundColor);
			option_text = ToColorString(OptionTextColor);
			search_match = ToColorString(SearchMatchColor);
			search_match_current = ToColorString(SearchMatchCurrentColor);
			tooltip = ToColorString(TooltipColor);
			warning_overlay = ToColorString(WarningOverlayColor);
			warning_background = ToColorString(WarningBackgroundColor);
			warning_message = ToColorString(WarningMessageColor);
		}

		public void OnAfterDeserialize()
		{
			ParseAndValidateColor(ref background, out BackgroundColor);
			ParseAndValidateColor(ref window_frame, out WindowFrameColor);
			ParseAndValidateColor(ref scrollbar, out ScrollbarColor);
			ParseAndValidateColor(ref button_text, out ButtonTextColor);
			ParseAndValidateColor(ref dropdown_background, out DropdownBackgroundColor);
			ParseAndValidateColor(ref dropdown_text, out DropdownTextColor);
			ParseAndValidateColor(ref dropdown_scrollbar, out DropdownScrollbarColor);
			ParseAndValidateColor(ref slider_background, out SliderBackgroundColor);
			ParseAndValidateColor(ref options_menu, out OptionsMenuColor);
			ParseAndValidateColor(ref option_background, out OptionBackgroundColor);
			ParseAndValidateColor(ref option_text, out OptionTextColor);
			ParseAndValidateColor(ref search_match, out SearchMatchColor);
			ParseAndValidateColor(ref search_match_current, out SearchMatchCurrentColor);
			ParseAndValidateColor(ref tooltip, out TooltipColor);
			ParseAndValidateColor(ref warning_overlay, out WarningOverlayColor);
			ParseAndValidateColor(ref warning_background, out WarningBackgroundColor);
			ParseAndValidateColor(ref warning_message, out WarningMessageColor);
		}
	}

	[Serializable]
	public class DocsColors : ISerializationCallbackReceiver
	{
		public TextInputColors text;

		[NonSerialized]
		public Color32 BackgroundColor;

		[ColorString]
		public string background;

		[NonSerialized]
		public Color32 SpoilerColor;

		[ColorString]
		public string spoiler;

		[NonSerialized]
		public Color32 SpoilerTextColor;

		[ColorString]
		public string spoiler_text;

		[NonSerialized]
		public Color32 LinkColor;

		[ColorString]
		public string link;

		[NonSerialized]
		public Color32 LinkHoverColor;

		[ColorString]
		public string link_hover;

		public void OnBeforeSerialize()
		{
			background = ToColorString(BackgroundColor);
			spoiler = ToColorString(SpoilerColor);
			spoiler_text = ToColorString(SpoilerTextColor);
			link = ToColorString(LinkColor);
			link_hover = ToColorString(LinkHoverColor);
		}

		public void OnAfterDeserialize()
		{
			ParseAndValidateColor(ref background, out BackgroundColor);
			ParseAndValidateColor(ref spoiler, out SpoilerColor);
			ParseAndValidateColor(ref spoiler_text, out SpoilerTextColor);
			ParseAndValidateColor(ref link, out LinkColor);
			ParseAndValidateColor(ref link_hover, out LinkHoverColor);
		}
	}

	[Serializable]
	public class CodeColors : ISerializationCallbackReceiver
	{
		public TextInputColors text;

		[NonSerialized]
		public Color32 ExecOverlayColor;

		[ColorString]
		public string exec_overlay;

		[NonSerialized]
		public Color32 WindowFrameExecColor;

		[ColorString]
		public string window_frame_exec;

		[NonSerialized]
		public Color32 BackgroundColor;

		[ColorString]
		public string background;

		public SelectableColors button_exec;

		[NonSerialized]
		public Color32 BreakpointColor;

		[ColorString]
		public string breakpoint;

		[NonSerialized]
		public Color32 BreakpointLineColor;

		[ColorString]
		public string breakpoint_line;

		[NonSerialized]
		public Color32 CommentColor;

		[ColorString]
		public string comment;

		[NonSerialized]
		public Color32 StringColor;

		[ColorString]
		public string @string;

		[NonSerialized]
		public Color32 NumberColor;

		[ColorString]
		public string number;

		[NonSerialized]
		public Color32 KeywordColor;

		[ColorString]
		public string keyword;

		[NonSerialized]
		public Color32 FunctionColor;

		[ColorString]
		public string function;

		[NonSerialized]
		public Color32 BuiltinColor;

		[ColorString]
		public string builtin;

		[NonSerialized]
		public Color32 CompleteBackgroundColor;

		[ColorString]
		public string complete_background;

		[NonSerialized]
		public Color32 CompleteTextColor;

		[ColorString]
		public string complete_text;

		[NonSerialized]
		public Color32 CompleteSelectedBackgroundColor;

		[ColorString]
		public string complete_selected_background;

		[NonSerialized]
		public Color32 CompleteSelectedTextColor;

		[ColorString]
		public string complete_selected_text;

		[NonSerialized]
		public Color32 ErrorBackgroundColor;

		[ColorString]
		public string error_background;

		[NonSerialized]
		public Color32 ErrorTitleColor;

		[ColorString]
		public string error_title;

		[NonSerialized]
		public Color32 ErrorMessageColor;

		[ColorString]
		public string error_message;

		[NonSerialized]
		public Color32 ErrorBorderColor;

		[ColorString]
		public string error_border;

		public void OnBeforeSerialize()
		{
			exec_overlay = ToColorString(ExecOverlayColor);
			window_frame_exec = ToColorString(WindowFrameExecColor);
			background = ToColorString(BackgroundColor);
			breakpoint = ToColorString(BreakpointColor);
			breakpoint_line = ToColorString(BreakpointLineColor);
			comment = ToColorString(CommentColor);
			@string = ToColorString(StringColor);
			number = ToColorString(NumberColor);
			keyword = ToColorString(KeywordColor);
			function = ToColorString(FunctionColor);
			builtin = ToColorString(BuiltinColor);
			complete_background = ToColorString(CompleteBackgroundColor);
			complete_text = ToColorString(CompleteTextColor);
			complete_selected_background = ToColorString(CompleteSelectedBackgroundColor);
			complete_selected_text = ToColorString(CompleteSelectedTextColor);
			error_background = ToColorString(ErrorBackgroundColor);
			error_title = ToColorString(ErrorTitleColor);
			error_message = ToColorString(ErrorMessageColor);
			error_border = ToColorString(ErrorBorderColor);
		}

		public void OnAfterDeserialize()
		{
			ParseAndValidateColor(ref exec_overlay, out ExecOverlayColor);
			ParseAndValidateColor(ref window_frame_exec, out WindowFrameExecColor);
			ParseAndValidateColor(ref background, out BackgroundColor);
			ParseAndValidateColor(ref breakpoint, out BreakpointColor);
			ParseAndValidateColor(ref breakpoint_line, out BreakpointLineColor);
			ParseAndValidateColor(ref comment, out CommentColor);
			ParseAndValidateColor(ref @string, out StringColor);
			ParseAndValidateColor(ref number, out NumberColor);
			ParseAndValidateColor(ref keyword, out KeywordColor);
			ParseAndValidateColor(ref function, out FunctionColor);
			ParseAndValidateColor(ref builtin, out BuiltinColor);
			ParseAndValidateColor(ref complete_background, out CompleteBackgroundColor);
			ParseAndValidateColor(ref complete_text, out CompleteTextColor);
			ParseAndValidateColor(ref complete_selected_background, out CompleteSelectedBackgroundColor);
			ParseAndValidateColor(ref complete_selected_text, out CompleteSelectedTextColor);
			ParseAndValidateColor(ref error_background, out ErrorBackgroundColor);
			ParseAndValidateColor(ref error_title, out ErrorTitleColor);
			ParseAndValidateColor(ref error_message, out ErrorMessageColor);
			ParseAndValidateColor(ref error_border, out ErrorBorderColor);
		}
	}

	[Serializable]
	public class UnlocksColors : ISerializationCallbackReceiver
	{
		[NonSerialized]
		public Color32 BackgroundColor;

		[ColorString]
		public string background;

		[NonSerialized]
		public Color32 BoxBackgroundColor;

		[ColorString]
		public string box_background;

		[NonSerialized]
		public Color32 NotUnlockableColor;

		[ColorString]
		public string not_unlockable;

		[NonSerialized]
		public Color32 AlreadyUnlockedColor;

		[ColorString]
		public string already_unlocked;

		public SelectableColors unaffordable;

		public SelectableColors unlockable;

		[NonSerialized]
		public Color32 LineLockedColor;

		[ColorString]
		public string line_locked;

		[NonSerialized]
		public Color32 LineUnlockedColor;

		[ColorString]
		public string line_unlocked;

		[NonSerialized]
		public Color32 UnlockCountBackgroundColor;

		[ColorString]
		public string unlock_count_background;

		[NonSerialized]
		public Color32 UnlockCountTextColor;

		[ColorString]
		public string unlock_count_text;

		public void OnBeforeSerialize()
		{
			background = ToColorString(BackgroundColor);
			box_background = ToColorString(BoxBackgroundColor);
			not_unlockable = ToColorString(NotUnlockableColor);
			already_unlocked = ToColorString(AlreadyUnlockedColor);
			line_locked = ToColorString(LineLockedColor);
			line_unlocked = ToColorString(LineUnlockedColor);
			unlock_count_background = ToColorString(UnlockCountBackgroundColor);
			unlock_count_text = ToColorString(UnlockCountTextColor);
		}

		public void OnAfterDeserialize()
		{
			ParseAndValidateColor(ref background, out BackgroundColor);
			ParseAndValidateColor(ref box_background, out BoxBackgroundColor);
			ParseAndValidateColor(ref not_unlockable, out NotUnlockableColor);
			ParseAndValidateColor(ref already_unlocked, out AlreadyUnlockedColor);
			ParseAndValidateColor(ref line_locked, out LineLockedColor);
			ParseAndValidateColor(ref line_unlocked, out LineUnlockedColor);
			ParseAndValidateColor(ref unlock_count_background, out UnlockCountBackgroundColor);
			ParseAndValidateColor(ref unlock_count_text, out UnlockCountTextColor);
		}
	}

	[Header("Properties")]
	public string theme_name;

	public string author;

	public int theme_version;

	private Uri _fileUri;

	[Header("Colors")]
	public UIColors ui;

	public DocsColors docs;

	public CodeColors code;

	public UnlocksColors unlocks;

	public bool IsBuiltin => FileUri == null;

	public Uri FileUri
	{
		get
		{
			return _fileUri;
		}
		set
		{
			if (!(_fileUri == value))
			{
				if (value != null && (!value.IsFile || !value.IsAbsoluteUri))
				{
					throw new ArgumentException("Invalid Theme File URI: Must be an absolute file path");
				}
				_fileUri = value;
			}
		}
	}

	public static string ToColorString(Color32 color)
	{
		return $"#{color.r:X2}{color.g:X2}{color.b:X2}{color.a:X2}";
	}

	public static void ParseAndValidateColor(ref string color, out Color32 value)
	{
		if (!TryParseColor(color, out value))
		{
			value = Color.black;
			color = "#000000ff";
		}
	}

	public static bool TryParseColor(string color, out Color32 value)
	{
		value = default(Color32);
		if (color == null || (color.Length != 7 && color.Length != 9))
		{
			return false;
		}
		if (color[0] != '#')
		{
			return false;
		}
		if (!byte.TryParse(MemoryExtensions.AsSpan(color, 1, 2), NumberStyles.HexNumber, null, out value.r))
		{
			return false;
		}
		if (!byte.TryParse(MemoryExtensions.AsSpan(color, 3, 2), NumberStyles.HexNumber, null, out value.g))
		{
			return false;
		}
		if (!byte.TryParse(MemoryExtensions.AsSpan(color, 5, 2), NumberStyles.HexNumber, null, out value.b))
		{
			return false;
		}
		if (color.Length < 9)
		{
			value.a = byte.MaxValue;
			return true;
		}
		return byte.TryParse(MemoryExtensions.AsSpan(color, 7, 2), NumberStyles.HexNumber, null, out value.a);
	}
}
