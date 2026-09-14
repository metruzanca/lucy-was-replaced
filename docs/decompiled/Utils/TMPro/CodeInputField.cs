using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TMPro;

[AddComponentMenu("UI/TextMeshPro - Input Field", 11)]
[HelpURL("https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/TextMeshPro/index.html")]
public class CodeInputField : Selectable, IUpdateSelectedHandler, IEventSystemHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, ISubmitHandler, ICancelHandler, ICanvasElement, ILayoutElement, IScrollHandler
{
	public enum ContentType
	{
		Standard,
		Autocorrected,
		IntegerNumber,
		DecimalNumber,
		Alphanumeric,
		Name,
		EmailAddress,
		Password,
		Pin,
		Custom
	}

	public enum InputType
	{
		Standard,
		AutoCorrect,
		Password
	}

	public enum CharacterValidation
	{
		None,
		Digit,
		Integer,
		Decimal,
		Alphanumeric,
		Name,
		Regex,
		EmailAddress,
		CustomValidator
	}

	public enum LineType
	{
		SingleLine,
		MultiLineSubmit,
		MultiLineNewline
	}

	public delegate char OnValidateInput(string text, int charIndex, char addedChar);

	[Serializable]
	public class SubmitEvent : UnityEvent<string>
	{
	}

	[Serializable]
	public class OnChangeEvent : UnityEvent<string>
	{
	}

	[Serializable]
	public class SelectionEvent : UnityEvent<string>
	{
	}

	[Serializable]
	public class TextSelectionEvent : UnityEvent<string, int, int>
	{
	}

	[Serializable]
	public class TouchScreenKeyboardEvent : UnityEvent<TouchScreenKeyboard.Status>
	{
	}

	protected enum EditState
	{
		Continue,
		Finish
	}

	protected TouchScreenKeyboard m_SoftKeyboard;

	private static readonly char[] kSeparators = new char[6] { ' ', '.', ',', '\t', '\r', '\n' };

	private static bool s_IsQuestDevice = false;

	protected RectTransform m_RectTransform;

	[SerializeField]
	protected RectTransform m_TextViewport;

	protected RectMask2D m_TextComponentRectMask;

	protected RectMask2D m_TextViewportRectMask;

	[SerializeField]
	protected TMP_Text m_TextComponent;

	protected RectTransform m_TextComponentRectTransform;

	[SerializeField]
	protected Graphic m_Placeholder;

	[SerializeField]
	protected Scrollbar m_VerticalScrollbar;

	[SerializeField]
	protected TMP_ScrollbarEventHandler m_VerticalScrollbarEventHandler;

	private bool m_IsDrivenByLayoutComponents;

	[SerializeField]
	private LayoutGroup m_LayoutGroup;

	private IScrollHandler m_IScrollHandlerParent;

	private float m_ScrollPosition;

	[SerializeField]
	protected float m_ScrollSensitivity = 1f;

	[SerializeField]
	private ContentType m_ContentType;

	[SerializeField]
	private InputType m_InputType;

	[SerializeField]
	private char m_AsteriskChar = '*';

	[SerializeField]
	private TouchScreenKeyboardType m_KeyboardType;

	[SerializeField]
	private LineType m_LineType;

	[SerializeField]
	private bool m_HideMobileInput;

	[SerializeField]
	private bool m_HideSoftKeyboard;

	[SerializeField]
	private CharacterValidation m_CharacterValidation;

	[SerializeField]
	private string m_RegexValue = string.Empty;

	[SerializeField]
	private float m_GlobalPointSize = 14f;

	[SerializeField]
	private int m_CharacterLimit;

	[SerializeField]
	private SubmitEvent m_OnEndEdit = new SubmitEvent();

	[SerializeField]
	private SubmitEvent m_OnSubmit = new SubmitEvent();

	[SerializeField]
	private SelectionEvent m_OnSelect = new SelectionEvent();

	[SerializeField]
	private SelectionEvent m_OnDeselect = new SelectionEvent();

	[SerializeField]
	private TextSelectionEvent m_OnTextSelection = new TextSelectionEvent();

	[SerializeField]
	private TextSelectionEvent m_OnEndTextSelection = new TextSelectionEvent();

	[SerializeField]
	private OnChangeEvent m_OnValueChanged = new OnChangeEvent();

	[SerializeField]
	private OnChangeEvent m_OnTextInserted = new OnChangeEvent();

	[SerializeField]
	private UnityEvent m_OnTextMeshUpdated = new UnityEvent();

	[SerializeField]
	private UnityEvent m_OnCarretMoved = new UnityEvent();

	[SerializeField]
	private UnityEvent m_OnBackSpace = new UnityEvent();

	[SerializeField]
	private UnityEvent m_OnClicked = new UnityEvent();

	[SerializeField]
	private TouchScreenKeyboardEvent m_OnTouchScreenKeyboardStatusChanged = new TouchScreenKeyboardEvent();

	[SerializeField]
	private OnValidateInput m_OnValidateInput;

	[SerializeField]
	private Color m_CaretColor = new Color(10f / 51f, 10f / 51f, 10f / 51f, 1f);

	[SerializeField]
	private bool m_CustomCaretColor;

	[SerializeField]
	private Color m_SelectionColor = new Color(56f / 85f, 0.80784315f, 1f, 64f / 85f);

	[SerializeField]
	[TextArea(5, 10)]
	protected string m_Text = string.Empty;

	[SerializeField]
	[Range(0f, 4f)]
	private float m_CaretBlinkRate = 0.85f;

	[SerializeField]
	[Range(1f, 5f)]
	private int m_CaretWidth = 1;

	[SerializeField]
	private bool m_ReadOnly;

	[SerializeField]
	private bool m_RichText = true;

	public RectTransform codeCompleter;

	public Camera uiCam;

	protected int m_StringPosition;

	protected int m_StringSelectPosition;

	protected int m_CaretPosition;

	protected int m_CaretSelectPosition;

	private RectTransform caretRectTrans;

	protected UIVertex[] m_CursorVerts;

	private CanvasRenderer m_CachedInputRenderer;

	private Vector2 m_LastPosition;

	[NonSerialized]
	protected Mesh m_Mesh;

	private bool m_AllowInput;

	private bool m_ShouldActivateNextUpdate;

	private bool m_UpdateDrag;

	private bool m_DragPositionOutOfBounds;

	private const float kHScrollSpeed = 0.05f;

	private const float kVScrollSpeed = 0.1f;

	protected bool m_CaretVisible;

	private Coroutine m_BlinkCoroutine;

	private float m_BlinkStartTime;

	private Coroutine m_DragCoroutine;

	private string m_OriginalText = "";

	private bool m_WasCanceled;

	private bool m_HasDoneFocusTransition;

	private WaitForSecondsRealtime m_WaitForSecondsRealtime;

	private bool m_PreventCallback;

	private bool m_TouchKeyboardAllowsInPlaceEditing;

	private bool m_IsTextComponentUpdateRequired;

	private bool m_HasTextBeenRemoved;

	private float m_PointerDownClickStartTime;

	private float m_KeyDownStartTime;

	private float m_DoubleClickDelay = 0.25f;

	private bool m_IsApplePlatform;

	public bool blockArrowKeys;

	private const string kEmailSpecialCharacters = "!#$%&'*+-/=?^_`{|}~";

	private const string kOculusQuestDeviceModel = "Oculus Quest";

	private bool m_IsCompositionActive;

	private bool m_ShouldUpdateIMEWindowPosition;

	private int m_PreviousIMEInsertionLine;

	[SerializeField]
	protected TMP_FontAsset m_GlobalFontAsset;

	[SerializeField]
	protected bool m_OnFocusSelectAll = true;

	protected bool m_isSelectAll;

	[SerializeField]
	protected bool m_ResetOnDeActivation = true;

	private bool m_SelectionStillActive;

	private bool m_ReleaseSelection;

	private KeyCode m_LastKeyCode;

	private GameObject m_PreviouslySelectedObject;

	[SerializeField]
	private bool m_KeepTextSelectionVisible;

	[SerializeField]
	private bool m_RestoreOriginalTextOnEscape = true;

	[SerializeField]
	protected bool m_isRichTextEditingAllowed;

	[SerializeField]
	protected int m_LineLimit;

	public bool isAlert;

	[SerializeField]
	protected TMP_InputValidator m_InputValidator;

	[SerializeField]
	private bool m_ShouldActivateOnSelect = true;

	private bool m_isSelected;

	private bool m_IsStringPositionDirty;

	private bool m_IsCaretPositionDirty;

	private bool m_forceRectTransformAdjustment;

	private bool m_IsKeyboardBeingClosedInHoloLens;

	private Event m_ProcessingEvent = new Event();

	private BaseInput inputSystem
	{
		get
		{
			if ((bool)EventSystem.current && (bool)EventSystem.current.currentInputModule)
			{
				return EventSystem.current.currentInputModule.input;
			}
			return null;
		}
	}

	private string compositionString
	{
		get
		{
			if (!(inputSystem != null))
			{
				return Input.compositionString;
			}
			return inputSystem.compositionString;
		}
	}

	private int compositionLength
	{
		get
		{
			if (m_ReadOnly)
			{
				return 0;
			}
			return compositionString.Length;
		}
	}

	protected Mesh mesh
	{
		get
		{
			if (m_Mesh == null)
			{
				m_Mesh = new Mesh();
			}
			return m_Mesh;
		}
	}

	public virtual bool shouldActivateOnSelect
	{
		get
		{
			if (m_ShouldActivateOnSelect)
			{
				return Application.platform != RuntimePlatform.tvOS;
			}
			return false;
		}
		set
		{
			m_ShouldActivateOnSelect = value;
		}
	}

	public bool shouldHideMobileInput
	{
		get
		{
			switch (Application.platform)
			{
			case RuntimePlatform.IPhonePlayer:
			case RuntimePlatform.Android:
			case RuntimePlatform.WebGLPlayer:
			case RuntimePlatform.tvOS:
				return m_HideMobileInput;
			default:
				return true;
			}
		}
		set
		{
			switch (Application.platform)
			{
			case RuntimePlatform.IPhonePlayer:
			case RuntimePlatform.Android:
			case RuntimePlatform.WebGLPlayer:
			case RuntimePlatform.tvOS:
				SetPropertyUtility.SetStruct(ref m_HideMobileInput, value);
				break;
			default:
				m_HideMobileInput = true;
				break;
			}
		}
	}

	public bool shouldHideSoftKeyboard
	{
		get
		{
			switch (Application.platform)
			{
			case RuntimePlatform.IPhonePlayer:
			case RuntimePlatform.Android:
			case RuntimePlatform.WebGLPlayer:
			case RuntimePlatform.MetroPlayerX86:
			case RuntimePlatform.MetroPlayerX64:
			case RuntimePlatform.MetroPlayerARM:
			case RuntimePlatform.PS4:
			case RuntimePlatform.tvOS:
			case RuntimePlatform.Switch:
			case RuntimePlatform.GameCoreXboxSeries:
			case RuntimePlatform.GameCoreXboxOne:
			case RuntimePlatform.PS5:
				return m_HideSoftKeyboard;
			default:
				return true;
			}
		}
		set
		{
			switch (Application.platform)
			{
			case RuntimePlatform.IPhonePlayer:
			case RuntimePlatform.Android:
			case RuntimePlatform.WebGLPlayer:
			case RuntimePlatform.MetroPlayerX86:
			case RuntimePlatform.MetroPlayerX64:
			case RuntimePlatform.MetroPlayerARM:
			case RuntimePlatform.PS4:
			case RuntimePlatform.tvOS:
			case RuntimePlatform.Switch:
			case RuntimePlatform.GameCoreXboxSeries:
			case RuntimePlatform.GameCoreXboxOne:
			case RuntimePlatform.PS5:
				SetPropertyUtility.SetStruct(ref m_HideSoftKeyboard, value);
				break;
			default:
				m_HideSoftKeyboard = true;
				break;
			}
			if (m_HideSoftKeyboard && m_SoftKeyboard != null && TouchScreenKeyboard.isSupported && m_SoftKeyboard.active)
			{
				m_SoftKeyboard.active = false;
				m_SoftKeyboard = null;
			}
		}
	}

	public string text
	{
		get
		{
			return m_Text;
		}
		set
		{
			SetText(value);
		}
	}

	public bool isFocused => m_AllowInput;

	public float caretBlinkRate
	{
		get
		{
			return m_CaretBlinkRate;
		}
		set
		{
			if (SetPropertyUtility.SetStruct(ref m_CaretBlinkRate, value) && m_AllowInput)
			{
				SetCaretActive();
			}
		}
	}

	public int caretWidth
	{
		get
		{
			return m_CaretWidth;
		}
		set
		{
			if (SetPropertyUtility.SetStruct(ref m_CaretWidth, value))
			{
				MarkGeometryAsDirty();
			}
		}
	}

	public RectTransform textViewport
	{
		get
		{
			return m_TextViewport;
		}
		set
		{
			SetPropertyUtility.SetClass(ref m_TextViewport, value);
		}
	}

	public TMP_Text textComponent
	{
		get
		{
			return m_TextComponent;
		}
		set
		{
			if (SetPropertyUtility.SetClass(ref m_TextComponent, value))
			{
				SetTextComponentWrapMode();
			}
		}
	}

	public Graphic placeholder
	{
		get
		{
			return m_Placeholder;
		}
		set
		{
			SetPropertyUtility.SetClass(ref m_Placeholder, value);
		}
	}

	public Scrollbar verticalScrollbar
	{
		get
		{
			return m_VerticalScrollbar;
		}
		set
		{
			if (m_VerticalScrollbar != null)
			{
				m_VerticalScrollbar.onValueChanged.RemoveListener(OnScrollbarValueChange);
			}
			SetPropertyUtility.SetClass(ref m_VerticalScrollbar, value);
			if ((bool)m_VerticalScrollbar)
			{
				m_VerticalScrollbar.onValueChanged.AddListener(OnScrollbarValueChange);
			}
		}
	}

	public float scrollSensitivity
	{
		get
		{
			return m_ScrollSensitivity;
		}
		set
		{
			if (SetPropertyUtility.SetStruct(ref m_ScrollSensitivity, value))
			{
				MarkGeometryAsDirty();
			}
		}
	}

	public Color caretColor
	{
		get
		{
			if (!customCaretColor)
			{
				return ((Graphic)(object)textComponent).color;
			}
			return m_CaretColor;
		}
		set
		{
			if (SetPropertyUtility.SetColor(ref m_CaretColor, value))
			{
				MarkGeometryAsDirty();
			}
		}
	}

	public bool customCaretColor
	{
		get
		{
			return m_CustomCaretColor;
		}
		set
		{
			if (m_CustomCaretColor != value)
			{
				m_CustomCaretColor = value;
				MarkGeometryAsDirty();
			}
		}
	}

	public Color selectionColor
	{
		get
		{
			return m_SelectionColor;
		}
		set
		{
			if (SetPropertyUtility.SetColor(ref m_SelectionColor, value))
			{
				MarkGeometryAsDirty();
			}
		}
	}

	public SubmitEvent onEndEdit
	{
		get
		{
			return m_OnEndEdit;
		}
		set
		{
			SetPropertyUtility.SetClass(ref m_OnEndEdit, value);
		}
	}

	public SubmitEvent onSubmit
	{
		get
		{
			return m_OnSubmit;
		}
		set
		{
			SetPropertyUtility.SetClass(ref m_OnSubmit, value);
		}
	}

	public SelectionEvent onSelect
	{
		get
		{
			return m_OnSelect;
		}
		set
		{
			SetPropertyUtility.SetClass(ref m_OnSelect, value);
		}
	}

	public SelectionEvent onDeselect
	{
		get
		{
			return m_OnDeselect;
		}
		set
		{
			SetPropertyUtility.SetClass(ref m_OnDeselect, value);
		}
	}

	public TextSelectionEvent onTextSelection
	{
		get
		{
			return m_OnTextSelection;
		}
		set
		{
			SetPropertyUtility.SetClass(ref m_OnTextSelection, value);
		}
	}

	public TextSelectionEvent onEndTextSelection
	{
		get
		{
			return m_OnEndTextSelection;
		}
		set
		{
			SetPropertyUtility.SetClass(ref m_OnEndTextSelection, value);
		}
	}

	public OnChangeEvent onValueChanged
	{
		get
		{
			return m_OnValueChanged;
		}
		set
		{
			SetPropertyUtility.SetClass(ref m_OnValueChanged, value);
		}
	}

	public OnChangeEvent onTextInserted
	{
		get
		{
			return m_OnTextInserted;
		}
		set
		{
			SetPropertyUtility.SetClass(ref m_OnTextInserted, value);
		}
	}

	public UnityEvent onTextMeshUpdated
	{
		get
		{
			return m_OnTextMeshUpdated;
		}
		set
		{
			SetPropertyUtility.SetClass(ref m_OnTextMeshUpdated, value);
		}
	}

	public UnityEvent onCarretMoved
	{
		get
		{
			return m_OnCarretMoved;
		}
		set
		{
			SetPropertyUtility.SetClass(ref m_OnCarretMoved, value);
		}
	}

	public UnityEvent onBackSpace
	{
		get
		{
			return m_OnBackSpace;
		}
		set
		{
			SetPropertyUtility.SetClass(ref m_OnBackSpace, value);
		}
	}

	public UnityEvent onClicked
	{
		get
		{
			return m_OnClicked;
		}
		set
		{
			SetPropertyUtility.SetClass(ref m_OnClicked, value);
		}
	}

	public TouchScreenKeyboardEvent onTouchScreenKeyboardStatusChanged
	{
		get
		{
			return m_OnTouchScreenKeyboardStatusChanged;
		}
		set
		{
			SetPropertyUtility.SetClass(ref m_OnTouchScreenKeyboardStatusChanged, value);
		}
	}

	public OnValidateInput onValidateInput
	{
		get
		{
			return m_OnValidateInput;
		}
		set
		{
			SetPropertyUtility.SetClass(ref m_OnValidateInput, value);
		}
	}

	public int characterLimit
	{
		get
		{
			return m_CharacterLimit;
		}
		set
		{
			if (SetPropertyUtility.SetStruct(ref m_CharacterLimit, Math.Max(0, value)))
			{
				UpdateLabel();
				if (m_SoftKeyboard != null)
				{
					m_SoftKeyboard.characterLimit = value;
				}
			}
		}
	}

	public float pointSize
	{
		get
		{
			return m_GlobalPointSize;
		}
		set
		{
			if (SetPropertyUtility.SetStruct(ref m_GlobalPointSize, Math.Max(0f, value)))
			{
				SetGlobalPointSize(m_GlobalPointSize);
				UpdateLabel();
			}
		}
	}

	public TMP_FontAsset fontAsset
	{
		get
		{
			return m_GlobalFontAsset;
		}
		set
		{
			if (SetPropertyUtility.SetClass(ref m_GlobalFontAsset, value))
			{
				SetGlobalFontAsset(m_GlobalFontAsset);
				UpdateLabel();
			}
		}
	}

	public bool onFocusSelectAll
	{
		get
		{
			return m_OnFocusSelectAll;
		}
		set
		{
			m_OnFocusSelectAll = value;
		}
	}

	public bool resetOnDeActivation
	{
		get
		{
			return m_ResetOnDeActivation;
		}
		set
		{
			m_ResetOnDeActivation = value;
		}
	}

	public bool keepTextSelectionVisible
	{
		get
		{
			return m_KeepTextSelectionVisible;
		}
		set
		{
			m_KeepTextSelectionVisible = value;
		}
	}

	public bool restoreOriginalTextOnEscape
	{
		get
		{
			return m_RestoreOriginalTextOnEscape;
		}
		set
		{
			m_RestoreOriginalTextOnEscape = value;
		}
	}

	public bool isRichTextEditingAllowed
	{
		get
		{
			return m_isRichTextEditingAllowed;
		}
		set
		{
			m_isRichTextEditingAllowed = value;
		}
	}

	public ContentType contentType
	{
		get
		{
			return m_ContentType;
		}
		set
		{
			if (SetPropertyUtility.SetStruct(ref m_ContentType, value))
			{
				EnforceContentType();
			}
		}
	}

	public LineType lineType
	{
		get
		{
			return m_LineType;
		}
		set
		{
			if (SetPropertyUtility.SetStruct(ref m_LineType, value))
			{
				SetToCustomIfContentTypeIsNot(ContentType.Standard, ContentType.Autocorrected);
				SetTextComponentWrapMode();
			}
		}
	}

	public int lineLimit
	{
		get
		{
			return m_LineLimit;
		}
		set
		{
			if (m_LineType == LineType.SingleLine)
			{
				m_LineLimit = 1;
			}
			else
			{
				SetPropertyUtility.SetStruct(ref m_LineLimit, value);
			}
		}
	}

	public InputType inputType
	{
		get
		{
			return m_InputType;
		}
		set
		{
			if (SetPropertyUtility.SetStruct(ref m_InputType, value))
			{
				SetToCustom();
			}
		}
	}

	public TouchScreenKeyboard touchScreenKeyboard => m_SoftKeyboard;

	public TouchScreenKeyboardType keyboardType
	{
		get
		{
			return m_KeyboardType;
		}
		set
		{
			if (SetPropertyUtility.SetStruct(ref m_KeyboardType, value))
			{
				SetToCustom();
			}
		}
	}

	public CharacterValidation characterValidation
	{
		get
		{
			return m_CharacterValidation;
		}
		set
		{
			if (SetPropertyUtility.SetStruct(ref m_CharacterValidation, value))
			{
				SetToCustom();
			}
		}
	}

	public TMP_InputValidator inputValidator
	{
		get
		{
			return m_InputValidator;
		}
		set
		{
			if (SetPropertyUtility.SetClass(ref m_InputValidator, value))
			{
				SetToCustom(CharacterValidation.CustomValidator);
			}
		}
	}

	public bool readOnly
	{
		get
		{
			return m_ReadOnly;
		}
		set
		{
			m_ReadOnly = value;
		}
	}

	public bool richText
	{
		get
		{
			return m_RichText;
		}
		set
		{
			m_RichText = value;
			SetTextComponentRichTextMode();
		}
	}

	public bool multiLine
	{
		get
		{
			if (m_LineType != LineType.MultiLineNewline)
			{
				return lineType == LineType.MultiLineSubmit;
			}
			return true;
		}
	}

	public char asteriskChar
	{
		get
		{
			return m_AsteriskChar;
		}
		set
		{
			if (SetPropertyUtility.SetStruct(ref m_AsteriskChar, value))
			{
				UpdateLabel();
			}
		}
	}

	public bool wasCanceled => m_WasCanceled;

	protected int caretPositionInternal
	{
		get
		{
			return m_CaretPosition + compositionLength;
		}
		set
		{
			m_CaretPosition = value;
			ClampCaretPos(ref m_CaretPosition);
		}
	}

	protected int stringPositionInternal
	{
		get
		{
			return m_StringPosition + compositionLength;
		}
		set
		{
			m_StringPosition = value;
			ClampStringPos(ref m_StringPosition);
		}
	}

	protected int caretSelectPositionInternal
	{
		get
		{
			return m_CaretSelectPosition + compositionLength;
		}
		set
		{
			m_CaretSelectPosition = value;
			ClampCaretPos(ref m_CaretSelectPosition);
		}
	}

	protected int stringSelectPositionInternal
	{
		get
		{
			return m_StringSelectPosition + compositionLength;
		}
		set
		{
			m_StringSelectPosition = value;
			ClampStringPos(ref m_StringSelectPosition);
		}
	}

	private bool hasSelection => stringPositionInternal != stringSelectPositionInternal;

	public int caretPosition
	{
		get
		{
			return caretSelectPositionInternal;
		}
		set
		{
			selectionAnchorPosition = value;
			selectionFocusPosition = value;
			UpdateStringIndexFromCaretPosition();
		}
	}

	public int selectionAnchorPosition
	{
		get
		{
			return caretPositionInternal;
		}
		set
		{
			if (compositionLength == 0)
			{
				caretPositionInternal = value;
				m_IsStringPositionDirty = true;
			}
		}
	}

	public int selectionFocusPosition
	{
		get
		{
			return caretSelectPositionInternal;
		}
		set
		{
			if (compositionLength == 0)
			{
				caretSelectPositionInternal = value;
				m_IsStringPositionDirty = true;
			}
		}
	}

	public int stringPosition
	{
		get
		{
			return stringSelectPositionInternal;
		}
		set
		{
			selectionStringAnchorPosition = value;
			selectionStringFocusPosition = value;
			UpdateCaretPositionFromStringIndex();
		}
	}

	public int selectionStringAnchorPosition
	{
		get
		{
			return stringPositionInternal;
		}
		set
		{
			if (compositionLength == 0)
			{
				stringPositionInternal = value;
				m_IsCaretPositionDirty = true;
				SendOnCarretMoved();
			}
		}
	}

	public int selectionStringFocusPosition
	{
		get
		{
			return stringSelectPositionInternal;
		}
		set
		{
			if (compositionLength == 0)
			{
				stringSelectPositionInternal = value;
				m_IsCaretPositionDirty = true;
			}
		}
	}

	private static string clipboard
	{
		get
		{
			return GUIUtility.systemCopyBuffer;
		}
		set
		{
			GUIUtility.systemCopyBuffer = value;
		}
	}

	public virtual float minWidth => 0f;

	public virtual float preferredWidth
	{
		get
		{
			if ((UnityEngine.Object)(object)textComponent == null)
			{
				return 0f;
			}
			float num = 0f;
			if (m_LayoutGroup != null)
			{
				num = m_LayoutGroup.padding.horizontal;
			}
			if (m_TextViewport != null)
			{
				num += m_TextViewport.offsetMin.x - m_TextViewport.offsetMax.x;
			}
			return m_TextComponent.preferredWidth + num;
		}
	}

	public virtual float flexibleWidth => -1f;

	public virtual float minHeight => 0f;

	public virtual float preferredHeight
	{
		get
		{
			if ((UnityEngine.Object)(object)textComponent == null)
			{
				return 0f;
			}
			float num = 0f;
			if (m_LayoutGroup != null)
			{
				num = m_LayoutGroup.padding.vertical;
			}
			if (m_TextViewport != null)
			{
				num += m_TextViewport.offsetMin.y - m_TextViewport.offsetMax.y;
			}
			return m_TextComponent.preferredHeight + num;
		}
	}

	public virtual float flexibleHeight => -1f;

	public virtual int layoutPriority => 1;

	protected CodeInputField()
	{
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Expected O, but got Unknown
		SetTextComponentWrapMode();
	}

	private bool isKeyboardUsingEvents()
	{
		switch (Application.platform)
		{
		case RuntimePlatform.Android:
			if (InPlaceEditing())
			{
				return m_HideSoftKeyboard;
			}
			return false;
		case RuntimePlatform.IPhonePlayer:
		case RuntimePlatform.tvOS:
			return m_HideSoftKeyboard;
		case RuntimePlatform.PS4:
		case RuntimePlatform.Switch:
		case RuntimePlatform.GameCoreXboxSeries:
		case RuntimePlatform.GameCoreXboxOne:
		case RuntimePlatform.PS5:
			return false;
		case RuntimePlatform.WebGLPlayer:
			if (m_SoftKeyboard != null)
			{
				return !m_SoftKeyboard.active;
			}
			return true;
		default:
			return true;
		}
	}

	private bool isUWP()
	{
		if (Application.platform != RuntimePlatform.MetroPlayerX86 && Application.platform != RuntimePlatform.MetroPlayerX64)
		{
			return Application.platform == RuntimePlatform.MetroPlayerARM;
		}
		return true;
	}

	public void SetTextWithoutNotify(string input)
	{
		SetText(input, sendCallback: false);
	}

	private void SetText(string value, bool sendCallback = true)
	{
		if (!(text == value))
		{
			if (value == null)
			{
				value = "";
			}
			value = value.Replace("\0", string.Empty);
			m_Text = value;
			if (m_SoftKeyboard != null)
			{
				m_SoftKeyboard.text = m_Text;
			}
			if (m_StringPosition > m_Text.Length)
			{
				m_StringPosition = (m_StringSelectPosition = m_Text.Length);
			}
			else if (m_StringSelectPosition > m_Text.Length)
			{
				m_StringSelectPosition = m_Text.Length;
			}
			m_forceRectTransformAdjustment = true;
			m_IsTextComponentUpdateRequired = true;
			UpdateLabel();
			if (sendCallback)
			{
				SendOnValueChanged();
				SendOnCarretMoved();
			}
		}
	}

	protected void ClampStringPos(ref int pos)
	{
		if (pos <= 0)
		{
			pos = 0;
		}
		else if (pos > text.Length)
		{
			pos = text.Length;
		}
	}

	protected void ClampCaretPos(ref int pos)
	{
		if (pos > m_TextComponent.textInfo.characterCount - 1)
		{
			pos = m_TextComponent.textInfo.characterCount - 1;
		}
		if (pos <= 0)
		{
			pos = 0;
		}
	}

	private int ClampArrayIndex(int index)
	{
		if (index < 0)
		{
			return 0;
		}
		return index;
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		if (m_Text == null)
		{
			m_Text = string.Empty;
		}
		m_IsApplePlatform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX || SystemInfo.operatingSystem.Contains("iOS") || SystemInfo.operatingSystem.Contains("tvOS");
		if (GetComponent<ILayoutController>() != null)
		{
			m_IsDrivenByLayoutComponents = true;
			m_LayoutGroup = GetComponent<LayoutGroup>();
		}
		else
		{
			m_IsDrivenByLayoutComponents = false;
		}
		if (Application.isPlaying && (UnityEngine.Object)(object)m_CachedInputRenderer == null && (UnityEngine.Object)(object)m_TextComponent != null)
		{
			GameObject gameObject = new GameObject("Caret", typeof(TMP_SelectionCaret));
			gameObject.hideFlags = HideFlags.DontSave;
			gameObject.transform.SetParent(m_TextComponent.transform.parent);
			gameObject.transform.SetAsFirstSibling();
			gameObject.layer = base.gameObject.layer;
			caretRectTrans = gameObject.GetComponent<RectTransform>();
			m_CachedInputRenderer = gameObject.GetComponent<CanvasRenderer>();
			m_CachedInputRenderer.SetMaterial(Graphic.defaultGraphicMaterial, (Texture)Texture2D.whiteTexture);
			gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
			AssignPositioningIfNeeded();
		}
		m_RectTransform = GetComponent<RectTransform>();
		IScrollHandler[] componentsInParent = GetComponentsInParent<IScrollHandler>();
		if (componentsInParent.Length > 1)
		{
			m_IScrollHandlerParent = componentsInParent[1] as ScrollRect;
		}
		if (m_TextViewport != null)
		{
			m_TextViewportRectMask = m_TextViewport.GetComponent<RectMask2D>();
			UpdateMaskRegions();
		}
		if ((UnityEngine.Object)(object)m_CachedInputRenderer != null)
		{
			m_CachedInputRenderer.SetMaterial(Graphic.defaultGraphicMaterial, (Texture)Texture2D.whiteTexture);
		}
		if ((UnityEngine.Object)(object)m_TextComponent != null)
		{
			((Graphic)(object)m_TextComponent).RegisterDirtyVerticesCallback((UnityAction)MarkGeometryAsDirty);
			((Graphic)(object)m_TextComponent).RegisterDirtyVerticesCallback((UnityAction)UpdateLabel);
			if (m_VerticalScrollbar != null)
			{
				m_VerticalScrollbar.onValueChanged.AddListener(OnScrollbarValueChange);
			}
			UpdateLabel();
		}
		m_TouchKeyboardAllowsInPlaceEditing = TouchScreenKeyboard.isInPlaceEditingAllowed;
		TMPro_EventManager.TEXT_CHANGED_EVENT.Add((Action<UnityEngine.Object>)ON_TEXT_CHANGED);
	}

	protected override void OnDisable()
	{
		m_BlinkCoroutine = null;
		DeactivateInputField();
		if ((UnityEngine.Object)(object)m_TextComponent != null)
		{
			((Graphic)(object)m_TextComponent).UnregisterDirtyVerticesCallback((UnityAction)MarkGeometryAsDirty);
			((Graphic)(object)m_TextComponent).UnregisterDirtyVerticesCallback((UnityAction)UpdateLabel);
			if (m_VerticalScrollbar != null)
			{
				m_VerticalScrollbar.onValueChanged.RemoveListener(OnScrollbarValueChange);
			}
		}
		CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);
		if ((UnityEngine.Object)(object)m_CachedInputRenderer != null)
		{
			m_CachedInputRenderer.Clear();
		}
		if (m_Mesh != null)
		{
			UnityEngine.Object.DestroyImmediate(m_Mesh);
		}
		m_Mesh = null;
		TMPro_EventManager.TEXT_CHANGED_EVENT.Remove((Action<UnityEngine.Object>)ON_TEXT_CHANGED);
		base.OnDisable();
	}

	private void ON_TEXT_CHANGED(UnityEngine.Object obj)
	{
		if (obj == (UnityEngine.Object)(object)m_TextComponent && !m_IsStringPositionDirty)
		{
			if (Application.isPlaying && compositionLength == 0)
			{
				UpdateCaretPositionFromStringIndex();
			}
			if ((bool)m_VerticalScrollbar)
			{
				UpdateScrollbar();
			}
		}
	}

	private IEnumerator CaretBlink()
	{
		m_CaretVisible = true;
		yield return null;
		while ((isFocused || m_SelectionStillActive) && m_CaretBlinkRate > 0f)
		{
			float num = 1f / m_CaretBlinkRate;
			bool flag = (Time.unscaledTime - m_BlinkStartTime) % num < num / 2f;
			if (m_CaretVisible != flag)
			{
				m_CaretVisible = flag;
				if (!hasSelection)
				{
					MarkGeometryAsDirty();
				}
			}
			yield return null;
		}
		m_BlinkCoroutine = null;
	}

	private void SetCaretVisible()
	{
		if (m_AllowInput)
		{
			m_CaretVisible = true;
			m_BlinkStartTime = Time.unscaledTime;
			SetCaretActive();
		}
	}

	private void SetCaretActive()
	{
		if (!m_AllowInput)
		{
			return;
		}
		if (m_CaretBlinkRate > 0f)
		{
			if (m_BlinkCoroutine == null)
			{
				m_BlinkCoroutine = StartCoroutine(CaretBlink());
			}
		}
		else
		{
			m_CaretVisible = true;
		}
	}

	protected void OnFocus()
	{
		if (m_OnFocusSelectAll)
		{
			SelectAll();
		}
	}

	protected void SelectAll()
	{
		m_isSelectAll = true;
		stringPositionInternal = text.Length;
		stringSelectPositionInternal = 0;
		SendOnCarretMoved();
	}

	public void MoveTextEnd(bool shift)
	{
		if (m_isRichTextEditingAllowed)
		{
			int length = text.Length;
			if (shift)
			{
				stringSelectPositionInternal = length;
			}
			else
			{
				stringPositionInternal = length;
				stringSelectPositionInternal = stringPositionInternal;
			}
		}
		else
		{
			int num = m_TextComponent.textInfo.characterCount - 1;
			if (shift)
			{
				caretSelectPositionInternal = num;
				stringSelectPositionInternal = GetStringIndexFromCaretPosition(num);
			}
			else
			{
				int num3 = (caretSelectPositionInternal = num);
				caretPositionInternal = num3;
				num3 = (stringPositionInternal = GetStringIndexFromCaretPosition(num));
				stringSelectPositionInternal = num3;
			}
		}
		SendOnCarretMoved();
		UpdateLabel();
	}

	public void MoveTextStart(bool shift)
	{
		if (m_isRichTextEditingAllowed)
		{
			int num = 0;
			if (shift)
			{
				stringSelectPositionInternal = num;
			}
			else
			{
				stringPositionInternal = num;
				stringSelectPositionInternal = stringPositionInternal;
			}
		}
		else
		{
			int num2 = 0;
			if (shift)
			{
				caretSelectPositionInternal = num2;
				stringSelectPositionInternal = GetStringIndexFromCaretPosition(num2);
			}
			else
			{
				int num4 = (caretSelectPositionInternal = num2);
				caretPositionInternal = num4;
				num4 = (stringPositionInternal = GetStringIndexFromCaretPosition(num2));
				stringSelectPositionInternal = num4;
			}
		}
		SendOnCarretMoved();
		UpdateLabel();
	}

	public void MoveToEndOfLine(bool shift, bool ctrl)
	{
		int lineNumber = m_TextComponent.textInfo.characterInfo[caretPositionInternal].lineNumber;
		int num = (ctrl ? (m_TextComponent.textInfo.characterCount - 1) : m_TextComponent.textInfo.lineInfo[lineNumber].lastCharacterIndex);
		int index = m_TextComponent.textInfo.characterInfo[num].index;
		if (shift)
		{
			stringSelectPositionInternal = index;
			caretSelectPositionInternal = num;
		}
		else
		{
			stringPositionInternal = index;
			stringSelectPositionInternal = stringPositionInternal;
			int num3 = (caretPositionInternal = num);
			caretSelectPositionInternal = num3;
		}
		SendOnCarretMoved();
		UpdateLabel();
	}

	public void MoveToStartOfLine(bool shift, bool ctrl)
	{
		int lineNumber = m_TextComponent.textInfo.characterInfo[caretPositionInternal].lineNumber;
		int num = ((!ctrl) ? m_TextComponent.textInfo.lineInfo[lineNumber].firstCharacterIndex : 0);
		int num2 = 0;
		if (num > 0)
		{
			num2 = m_TextComponent.textInfo.characterInfo[num - 1].index + m_TextComponent.textInfo.characterInfo[num - 1].stringLength;
		}
		if (shift)
		{
			stringSelectPositionInternal = num2;
			caretSelectPositionInternal = num;
		}
		else
		{
			stringPositionInternal = num2;
			stringSelectPositionInternal = stringPositionInternal;
			int num4 = (caretPositionInternal = num);
			caretSelectPositionInternal = num4;
		}
		SendOnCarretMoved();
		UpdateLabel();
	}

	private bool InPlaceEditing()
	{
		if (m_TouchKeyboardAllowsInPlaceEditing)
		{
			return true;
		}
		if (isUWP())
		{
			return !TouchScreenKeyboard.isSupported;
		}
		if (TouchScreenKeyboard.isSupported && shouldHideSoftKeyboard)
		{
			return true;
		}
		if (TouchScreenKeyboard.isSupported && !shouldHideSoftKeyboard && !shouldHideMobileInput)
		{
			return false;
		}
		return true;
	}

	private bool InPlaceEditingChanged()
	{
		if (!s_IsQuestDevice)
		{
			return m_TouchKeyboardAllowsInPlaceEditing != TouchScreenKeyboard.isInPlaceEditingAllowed;
		}
		return false;
	}

	private bool TouchScreenKeyboardShouldBeUsed()
	{
		RuntimePlatform platform = Application.platform;
		if (platform == RuntimePlatform.Android || platform == RuntimePlatform.WebGLPlayer)
		{
			if (s_IsQuestDevice)
			{
				return TouchScreenKeyboard.isSupported;
			}
			return !TouchScreenKeyboard.isInPlaceEditingAllowed;
		}
		return TouchScreenKeyboard.isSupported;
	}

	private void UpdateKeyboardStringPosition()
	{
		if (m_HideMobileInput && m_SoftKeyboard != null && m_SoftKeyboard.canSetSelection && (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS))
		{
			int start = Mathf.Min(stringSelectPositionInternal, stringPositionInternal);
			int length = Mathf.Abs(stringSelectPositionInternal - stringPositionInternal);
			m_SoftKeyboard.selection = new RangeInt(start, length);
		}
	}

	private void UpdateStringPositionFromKeyboard()
	{
		RangeInt selection = m_SoftKeyboard.selection;
		int start = selection.start;
		int end = selection.end;
		bool flag = false;
		if (stringPositionInternal != start)
		{
			flag = true;
			stringPositionInternal = start;
			caretPositionInternal = GetCaretPositionFromStringIndex(stringPositionInternal);
		}
		if (stringSelectPositionInternal != end)
		{
			stringSelectPositionInternal = end;
			flag = true;
			caretSelectPositionInternal = GetCaretPositionFromStringIndex(stringSelectPositionInternal);
		}
		if (flag)
		{
			m_BlinkStartTime = Time.unscaledTime;
			SendOnCarretMoved();
			UpdateLabel();
		}
	}

	protected virtual void LateUpdate()
	{
		if (m_ShouldActivateNextUpdate)
		{
			if (!isFocused)
			{
				ActivateInputFieldInternal();
				m_ShouldActivateNextUpdate = false;
				return;
			}
			m_ShouldActivateNextUpdate = false;
		}
		if (isFocused && InPlaceEditingChanged())
		{
			DeactivateInputField();
		}
		if (!isFocused && m_SelectionStillActive)
		{
			GameObject gameObject = ((EventSystem.current != null) ? EventSystem.current.currentSelectedGameObject : null);
			if (gameObject == null && m_ResetOnDeActivation)
			{
				ReleaseSelection();
				return;
			}
			if (gameObject != null && gameObject != base.gameObject)
			{
				if (gameObject == m_PreviouslySelectedObject)
				{
					return;
				}
				m_PreviouslySelectedObject = gameObject;
				if (!m_VerticalScrollbar || !(gameObject == m_VerticalScrollbar.gameObject))
				{
					if (m_ResetOnDeActivation)
					{
						ReleaseSelection();
					}
					else if (!m_KeepTextSelectionVisible && gameObject.GetComponent<CodeInputField>() != null)
					{
						ReleaseSelection();
					}
				}
				return;
			}
			if (Input.GetKeyDown(KeyCode.Mouse0))
			{
				bool flag = false;
				float unscaledTime = Time.unscaledTime;
				if (m_KeyDownStartTime + m_DoubleClickDelay > unscaledTime)
				{
					flag = true;
				}
				m_KeyDownStartTime = unscaledTime;
				if (flag)
				{
					ReleaseSelection();
					return;
				}
			}
		}
		UpdateMaskRegions();
		if ((InPlaceEditing() && isKeyboardUsingEvents()) || !isFocused)
		{
			return;
		}
		AssignPositioningIfNeeded();
		if (m_SoftKeyboard == null || m_SoftKeyboard.status != 0)
		{
			if (m_SoftKeyboard != null)
			{
				if (!m_ReadOnly)
				{
					this.text = m_SoftKeyboard.text;
				}
				TouchScreenKeyboard.Status status = m_SoftKeyboard.status;
				if (m_LastKeyCode != KeyCode.Return && status == TouchScreenKeyboard.Status.Done && isUWP())
				{
					status = TouchScreenKeyboard.Status.Canceled;
					m_IsKeyboardBeingClosedInHoloLens = true;
				}
				switch (status)
				{
				case TouchScreenKeyboard.Status.LostFocus:
					SendTouchScreenKeyboardStatusChanged();
					break;
				case TouchScreenKeyboard.Status.Canceled:
					m_ReleaseSelection = true;
					m_WasCanceled = true;
					SendTouchScreenKeyboardStatusChanged();
					break;
				case TouchScreenKeyboard.Status.Done:
					m_ReleaseSelection = true;
					SendTouchScreenKeyboardStatusChanged();
					OnSubmit(null);
					break;
				}
			}
			OnDeselect(null);
			return;
		}
		string text = m_SoftKeyboard.text;
		if (m_Text != text)
		{
			if (m_ReadOnly)
			{
				m_SoftKeyboard.text = m_Text;
			}
			else
			{
				m_Text = "";
				for (int i = 0; i < text.Length; i++)
				{
					char c = text[i];
					bool flag2 = false;
					if (c == '\r' || c == '\u0003')
					{
						c = '\n';
					}
					if (onValidateInput != null)
					{
						c = onValidateInput(m_Text, m_Text.Length, c);
					}
					else if (characterValidation != 0)
					{
						string obj = m_Text;
						c = Validate(m_Text, m_Text.Length, c);
						flag2 = obj != m_Text;
					}
					if (lineType != LineType.MultiLineNewline && c == '\n')
					{
						UpdateLabel();
						OnSubmit(null);
						OnDeselect(null);
						return;
					}
					if (c != 0 && (characterValidation != CharacterValidation.CustomValidator || !flag2))
					{
						m_Text += c;
					}
				}
				if (characterLimit > 0 && m_Text.Length > characterLimit)
				{
					m_Text = m_Text.Substring(0, characterLimit);
				}
				UpdateStringPositionFromKeyboard();
				if (m_Text != text)
				{
					m_SoftKeyboard.text = m_Text;
				}
				SendOnValueChangedAndUpdateLabel();
			}
		}
		else if (m_HideMobileInput && m_SoftKeyboard != null && m_SoftKeyboard.canSetSelection && Application.platform != RuntimePlatform.IPhonePlayer && Application.platform != RuntimePlatform.tvOS)
		{
			int start = Mathf.Min(stringSelectPositionInternal, stringPositionInternal);
			int length = Mathf.Abs(stringSelectPositionInternal - stringPositionInternal);
			m_SoftKeyboard.selection = new RangeInt(start, length);
		}
		else if ((m_HideMobileInput && Application.platform == RuntimePlatform.Android) || (m_SoftKeyboard.canSetSelection && (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS)))
		{
			UpdateStringPositionFromKeyboard();
		}
		if (m_SoftKeyboard != null && m_SoftKeyboard.status != 0)
		{
			if (m_SoftKeyboard.status == TouchScreenKeyboard.Status.Canceled)
			{
				m_WasCanceled = true;
			}
			OnDeselect(null);
		}
	}

	private bool MayDrag(PointerEventData eventData)
	{
		if (IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left && (UnityEngine.Object)(object)m_TextComponent != null)
		{
			if (m_SoftKeyboard != null && !shouldHideSoftKeyboard)
			{
				return shouldHideMobileInput;
			}
			return true;
		}
		return false;
	}

	public virtual void OnBeginDrag(PointerEventData eventData)
	{
		if (MayDrag(eventData))
		{
			m_UpdateDrag = true;
		}
	}

	public virtual void OnDrag(PointerEventData eventData)
	{
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Invalid comparison between Unknown and I4
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Invalid comparison between Unknown and I4
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Invalid comparison between Unknown and I4
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Invalid comparison between Unknown and I4
		if (!MayDrag(eventData))
		{
			return;
		}
		CaretPosition val = default(CaretPosition);
		int cursorIndexFromPosition = TMP_TextUtilities.GetCursorIndexFromPosition(m_TextComponent, (Vector3)eventData.position, eventData.pressEventCamera, ref val);
		if (m_isRichTextEditingAllowed)
		{
			if ((int)val == 1)
			{
				stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition].index;
			}
			else if ((int)val == 2)
			{
				stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition].index + m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition].stringLength;
			}
		}
		else if ((int)val == 1)
		{
			stringSelectPositionInternal = ((cursorIndexFromPosition == 0) ? m_TextComponent.textInfo.characterInfo[0].index : (m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition - 1].index + m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition - 1].stringLength));
		}
		else if ((int)val == 2)
		{
			stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition].index + m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition].stringLength;
		}
		caretSelectPositionInternal = GetCaretPositionFromStringIndex(stringSelectPositionInternal);
		MarkGeometryAsDirty();
		m_DragPositionOutOfBounds = !RectTransformUtility.RectangleContainsScreenPoint(textViewport, eventData.position, eventData.pressEventCamera);
		if (m_DragPositionOutOfBounds && m_DragCoroutine == null)
		{
			m_DragCoroutine = StartCoroutine(MouseDragOutsideRect(eventData));
		}
		UpdateKeyboardStringPosition();
		eventData.Use();
	}

	private IEnumerator MouseDragOutsideRect(PointerEventData eventData)
	{
		Vector2 vector = default(Vector2);
		while (m_UpdateDrag && m_DragPositionOutOfBounds)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(textViewport, eventData.position, eventData.pressEventCamera, ref vector);
			Rect rect = textViewport.rect;
			if (multiLine)
			{
				if (vector.y > rect.yMax)
				{
					MoveUp(shift: true, goToFirstChar: true);
				}
				else if (vector.y < rect.yMin)
				{
					MoveDown(shift: true, goToLastChar: true);
				}
			}
			else if (vector.x < rect.xMin)
			{
				MoveLeft(shift: true, ctrl: false);
			}
			else if (vector.x > rect.xMax)
			{
				MoveRight(shift: true, ctrl: false);
			}
			UpdateLabel();
			float num = (multiLine ? 0.1f : 0.05f);
			if (m_WaitForSecondsRealtime == null)
			{
				m_WaitForSecondsRealtime = new WaitForSecondsRealtime(num);
			}
			else
			{
				m_WaitForSecondsRealtime.waitTime = num;
			}
			yield return m_WaitForSecondsRealtime;
		}
		m_DragCoroutine = null;
	}

	public virtual void OnEndDrag(PointerEventData eventData)
	{
		if (MayDrag(eventData))
		{
			m_UpdateDrag = false;
		}
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a4: Invalid comparison between Unknown and I4
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Invalid comparison between Unknown and I4
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Invalid comparison between Unknown and I4
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Invalid comparison between Unknown and I4
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_031a: Invalid comparison between Unknown and I4
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Invalid comparison between Unknown and I4
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Invalid comparison between Unknown and I4
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Invalid comparison between Unknown and I4
		if (!MayDrag(eventData))
		{
			return;
		}
		EventSystem.current.SetSelectedGameObject(base.gameObject, eventData);
		bool allowInput = m_AllowInput;
		base.OnPointerDown(eventData);
		onClicked?.Invoke();
		if (!InPlaceEditing() && (m_SoftKeyboard == null || !m_SoftKeyboard.active))
		{
			OnSelect(eventData);
			return;
		}
		bool flag = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
		bool flag2 = false;
		float unscaledTime = Time.unscaledTime;
		if (m_PointerDownClickStartTime + m_DoubleClickDelay > unscaledTime)
		{
			flag2 = true;
		}
		m_PointerDownClickStartTime = unscaledTime;
		if (allowInput || !m_OnFocusSelectAll)
		{
			CaretPosition val = default(CaretPosition);
			int cursorIndexFromPosition = TMP_TextUtilities.GetCursorIndexFromPosition(m_TextComponent, (Vector3)eventData.position, eventData.pressEventCamera, ref val);
			if (flag)
			{
				if (m_isRichTextEditingAllowed)
				{
					if ((int)val == 1)
					{
						stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition].index;
					}
					else if ((int)val == 2)
					{
						stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition].index + m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition].stringLength;
					}
				}
				else if ((int)val == 1)
				{
					stringSelectPositionInternal = ((cursorIndexFromPosition == 0) ? m_TextComponent.textInfo.characterInfo[0].index : (m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition - 1].index + m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition - 1].stringLength));
				}
				else if ((int)val == 2)
				{
					stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition].index + m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition].stringLength;
				}
			}
			else if (m_isRichTextEditingAllowed)
			{
				if ((int)val == 1)
				{
					int num = (stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition].index);
					stringPositionInternal = num;
				}
				else if ((int)val == 2)
				{
					int num = (stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition].index + m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition].stringLength);
					stringPositionInternal = num;
				}
			}
			else if ((int)val == 1)
			{
				int num = (stringSelectPositionInternal = ((cursorIndexFromPosition == 0) ? m_TextComponent.textInfo.characterInfo[0].index : (m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition - 1].index + m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition - 1].stringLength)));
				stringPositionInternal = num;
			}
			else if ((int)val == 2)
			{
				int num = (stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition].index + m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition].stringLength);
				stringPositionInternal = num;
			}
			if (flag2)
			{
				int num5 = TMP_TextUtilities.FindIntersectingWord(m_TextComponent, (Vector3)eventData.position, eventData.pressEventCamera);
				if (num5 != -1)
				{
					caretPositionInternal = m_TextComponent.textInfo.wordInfo[num5].firstCharacterIndex;
					caretSelectPositionInternal = m_TextComponent.textInfo.wordInfo[num5].lastCharacterIndex + 1;
					while (caretPositionInternal > 0 && IsIdentifierChar(m_TextComponent.textInfo.characterInfo[caretPositionInternal - 1].character))
					{
						caretPositionInternal--;
					}
					while (caretSelectPositionInternal < m_TextComponent.textInfo.characterCount && IsIdentifierChar(m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal].character))
					{
						caretSelectPositionInternal++;
					}
					stringPositionInternal = m_TextComponent.textInfo.characterInfo[caretPositionInternal].index;
					stringSelectPositionInternal = m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal - 1].index + m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal - 1].stringLength;
				}
				else
				{
					caretPositionInternal = cursorIndexFromPosition;
					caretSelectPositionInternal = caretPositionInternal + 1;
					stringPositionInternal = m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition].index;
					stringSelectPositionInternal = stringPositionInternal + m_TextComponent.textInfo.characterInfo[cursorIndexFromPosition].stringLength;
				}
			}
			else
			{
				int num = (caretSelectPositionInternal = GetCaretPositionFromStringIndex(stringPositionInternal));
				caretPositionInternal = num;
			}
			m_isSelectAll = false;
		}
		SendOnCarretMoved();
		UpdateLabel();
		UpdateKeyboardStringPosition();
		eventData.Use();
	}

	private bool IsIdentifierChar(char c)
	{
		if (!char.IsLetterOrDigit(c))
		{
			return c == '_';
		}
		return true;
	}

	protected EditState KeyPressed(Event evt)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Invalid comparison between Unknown and I4
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Invalid comparison between Unknown and I4
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Invalid comparison between Unknown and I4
		EventModifiers modifiers = evt.modifiers;
		bool flag = (m_IsApplePlatform ? ((modifiers & 8) > 0) : ((modifiers & 2) > 0));
		bool flag2 = (modifiers & 1) > 0;
		bool flag3 = (modifiers & 4) > 0;
		bool flag4 = flag && !flag3 && !flag2;
		m_LastKeyCode = evt.keyCode;
		switch (evt.keyCode)
		{
		case KeyCode.Backspace:
			Backspace();
			return EditState.Continue;
		case KeyCode.Delete:
			DeleteKey();
			return EditState.Continue;
		case KeyCode.Home:
			MoveToStartOfLine(flag2, flag);
			return EditState.Continue;
		case KeyCode.End:
			MoveToEndOfLine(flag2, flag);
			return EditState.Continue;
		case KeyCode.A:
			if (flag4)
			{
				SelectAll();
				return EditState.Continue;
			}
			break;
		case KeyCode.C:
			if (flag4)
			{
				if (inputType != InputType.Password)
				{
					clipboard = Helper.StripRichText(GetSelectedString());
				}
				else
				{
					clipboard = "";
				}
				return EditState.Continue;
			}
			break;
		case KeyCode.V:
			if (flag4)
			{
				Append(clipboard);
				return EditState.Continue;
			}
			break;
		case KeyCode.X:
			if (flag4)
			{
				if (inputType != InputType.Password)
				{
					clipboard = GetSelectedString();
				}
				else
				{
					clipboard = "";
				}
				Delete();
				UpdateTouchKeyboardFromEditChanges();
				SendOnValueChangedAndUpdateLabel();
				return EditState.Continue;
			}
			break;
		case KeyCode.LeftArrow:
			MoveLeft(flag2, flag);
			return EditState.Continue;
		case KeyCode.RightArrow:
			MoveRight(flag2, flag);
			return EditState.Continue;
		case KeyCode.UpArrow:
			if (!blockArrowKeys)
			{
				MoveUp(flag2);
			}
			return EditState.Continue;
		case KeyCode.DownArrow:
			if (!blockArrowKeys)
			{
				MoveDown(flag2);
			}
			return EditState.Continue;
		case KeyCode.PageUp:
			return EditState.Continue;
		case KeyCode.PageDown:
			return EditState.Continue;
		case KeyCode.Return:
		case KeyCode.KeypadEnter:
		{
			if (lineType != LineType.MultiLineNewline)
			{
				m_ReleaseSelection = true;
				return EditState.Finish;
			}
			TMP_TextInfo textInfo = m_TextComponent.textInfo;
			if (m_LineLimit > 0 && textInfo != null && textInfo.lineCount >= m_LineLimit)
			{
				m_ReleaseSelection = true;
				return EditState.Finish;
			}
			break;
		}
		case KeyCode.Escape:
			m_ReleaseSelection = true;
			m_WasCanceled = true;
			return EditState.Finish;
		}
		char c = evt.character;
		if (!multiLine && (c == '\t' || c == '\r' || c == '\n'))
		{
			return EditState.Continue;
		}
		if (c == '\r' || c == '\u0003')
		{
			c = '\n';
		}
		if (IsValidChar(c))
		{
			Append(c);
			SendOnTextInserted(c.ToString());
		}
		if (c == '\0' && compositionLength > 0)
		{
			UpdateLabel();
		}
		return EditState.Continue;
	}

	protected virtual bool IsValidChar(char c)
	{
		if (c == '\u007f')
		{
			return false;
		}
		if (c == '\t' || c == '\n')
		{
			return true;
		}
		if (c < ' ')
		{
			return false;
		}
		return true;
	}

	public void ProcessEvent(Event e)
	{
		KeyPressed(e);
	}

	public virtual void OnUpdateSelected(BaseEventData eventData)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Invalid comparison between Unknown and I4
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Invalid comparison between Unknown and I4
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Invalid comparison between Unknown and I4
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		if (!isFocused)
		{
			return;
		}
		bool flag = false;
		while (Event.PopEvent(m_ProcessingEvent))
		{
			EventType rawType = m_ProcessingEvent.rawType;
			if ((int)rawType == 5)
			{
				continue;
			}
			if ((int)rawType == 4)
			{
				flag = true;
				if (m_IsCompositionActive && compositionLength == 0 && m_ProcessingEvent.character == '\0' && (int)m_ProcessingEvent.modifiers == 0)
				{
					continue;
				}
				if (KeyPressed(m_ProcessingEvent) == EditState.Finish)
				{
					if (!m_WasCanceled)
					{
						SendOnSubmit();
					}
					DeactivateInputField();
					break;
				}
				m_IsTextComponentUpdateRequired = true;
				UpdateLabel();
			}
			else if (rawType - 13 <= 1 && m_ProcessingEvent.commandName == "SelectAll")
			{
				SelectAll();
				flag = true;
			}
		}
		if (flag)
		{
			UpdateLabel();
			eventData.Use();
		}
	}

	public virtual void OnScroll(PointerEventData eventData)
	{
		if (m_LineType == LineType.SingleLine)
		{
			if (m_IScrollHandlerParent != null)
			{
				m_IScrollHandlerParent.OnScroll(eventData);
			}
		}
		else if (!(m_TextComponent.preferredHeight < m_TextViewport.rect.height))
		{
			float num = 0f - eventData.scrollDelta.y;
			m_ScrollPosition = GetScrollPositionRelativeToViewport();
			m_ScrollPosition += 1f / (float)m_TextComponent.textInfo.lineCount * num * m_ScrollSensitivity;
			m_ScrollPosition = Mathf.Clamp01(m_ScrollPosition);
			AdjustTextPositionRelativeToViewport(m_ScrollPosition);
			if ((bool)m_VerticalScrollbar)
			{
				m_VerticalScrollbar.value = m_ScrollPosition;
			}
		}
	}

	private float GetScrollPositionRelativeToViewport()
	{
		Rect rect = m_TextViewport.rect;
		return (float)(int)((m_TextComponent.textInfo.lineInfo[0].ascender + m_TextComponent.margin.y + m_TextComponent.margin.w - rect.yMax + m_TextComponent.rectTransform.anchoredPosition.y) / (m_TextComponent.preferredHeight - rect.height) * 1000f + 0.5f) / 1000f;
	}

	private string GetSelectedString()
	{
		if (!hasSelection)
		{
			return "";
		}
		int num = stringPositionInternal;
		int num2 = stringSelectPositionInternal;
		if (num > num2)
		{
			int num3 = num;
			num = num2;
			num2 = num3;
		}
		return text.Substring(num, num2 - num);
	}

	private int FindNextWordBegin()
	{
		if (stringSelectPositionInternal + 1 >= text.Length)
		{
			return text.Length;
		}
		int num = text.IndexOfAny(kSeparators, stringSelectPositionInternal + 1);
		if (num == -1)
		{
			return text.Length;
		}
		return num + 1;
	}

	private void MoveRight(bool shift, bool ctrl)
	{
		if (hasSelection && !shift)
		{
			int num2 = (stringSelectPositionInternal = Mathf.Max(stringPositionInternal, stringSelectPositionInternal));
			stringPositionInternal = num2;
			num2 = (caretSelectPositionInternal = GetCaretPositionFromStringIndex(stringSelectPositionInternal));
			caretPositionInternal = num2;
			return;
		}
		int num3 = (ctrl ? FindNextWordBegin() : (m_isRichTextEditingAllowed ? ((stringSelectPositionInternal >= text.Length || !char.IsHighSurrogate(text[stringSelectPositionInternal])) ? (stringSelectPositionInternal + 1) : (stringSelectPositionInternal + 2)) : ((m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal].character != '\r' || m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal + 1].character != '\n') ? (m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal].index + m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal].stringLength) : (m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal + 1].index + m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal + 1].stringLength))));
		if (shift)
		{
			stringSelectPositionInternal = num3;
			caretSelectPositionInternal = GetCaretPositionFromStringIndex(stringSelectPositionInternal);
		}
		else
		{
			int num2 = (stringPositionInternal = num3);
			stringSelectPositionInternal = num2;
			if (stringPositionInternal >= m_TextComponent.textInfo.characterInfo[caretPositionInternal].index + m_TextComponent.textInfo.characterInfo[caretPositionInternal].stringLength)
			{
				num2 = (caretPositionInternal = GetCaretPositionFromStringIndex(stringSelectPositionInternal));
				caretSelectPositionInternal = num2;
			}
		}
		SendOnCarretMoved();
	}

	private int FindPrevWordBegin()
	{
		if (stringSelectPositionInternal - 2 < 0)
		{
			return 0;
		}
		int num = text.LastIndexOfAny(kSeparators, stringSelectPositionInternal - 2);
		if (num == -1)
		{
			return 0;
		}
		return num + 1;
	}

	private void MoveLeft(bool shift, bool ctrl)
	{
		if (hasSelection && !shift)
		{
			int num2 = (stringSelectPositionInternal = Mathf.Min(stringPositionInternal, stringSelectPositionInternal));
			stringPositionInternal = num2;
			num2 = (caretSelectPositionInternal = GetCaretPositionFromStringIndex(stringSelectPositionInternal));
			caretPositionInternal = num2;
			return;
		}
		int num3;
		if (ctrl)
		{
			num3 = FindPrevWordBegin();
		}
		else if (m_isRichTextEditingAllowed)
		{
			num3 = ((stringSelectPositionInternal <= 0 || !char.IsLowSurrogate(text[stringSelectPositionInternal - 1])) ? (stringSelectPositionInternal - 1) : (stringSelectPositionInternal - 2));
		}
		else
		{
			num3 = ((caretSelectPositionInternal < 1) ? m_TextComponent.textInfo.characterInfo[0].index : m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal - 1].index);
			if (num3 > 0 && m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal - 1].character == '\n' && m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal - 2].character == '\r')
			{
				num3 = m_TextComponent.textInfo.characterInfo[caretSelectPositionInternal - 2].index;
			}
		}
		if (shift)
		{
			stringSelectPositionInternal = num3;
			caretSelectPositionInternal = GetCaretPositionFromStringIndex(stringSelectPositionInternal);
		}
		else
		{
			int num2 = (stringPositionInternal = num3);
			stringSelectPositionInternal = num2;
			if (caretPositionInternal > 0 && stringPositionInternal <= m_TextComponent.textInfo.characterInfo[caretPositionInternal - 1].index)
			{
				num2 = (caretPositionInternal = GetCaretPositionFromStringIndex(stringSelectPositionInternal));
				caretSelectPositionInternal = num2;
			}
		}
		SendOnCarretMoved();
	}

	private int LineUpCharacterPosition(int originalPos, bool goToFirstChar)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		if (originalPos >= m_TextComponent.textInfo.characterCount)
		{
			originalPos--;
		}
		TMP_CharacterInfo val = m_TextComponent.textInfo.characterInfo[originalPos];
		int lineNumber = val.lineNumber;
		if (lineNumber - 1 < 0)
		{
			if (!goToFirstChar)
			{
				return originalPos;
			}
			return 0;
		}
		int num = m_TextComponent.textInfo.lineInfo[lineNumber].firstCharacterIndex - 1;
		int num2 = -1;
		float num3 = 32767f;
		float num4 = 0f;
		for (int i = m_TextComponent.textInfo.lineInfo[lineNumber - 1].firstCharacterIndex; i < num; i++)
		{
			TMP_CharacterInfo val2 = m_TextComponent.textInfo.characterInfo[i];
			float num5 = val.origin - val2.origin;
			float num6 = num5 / (val2.xAdvance - val2.origin);
			if (num6 >= 0f && num6 <= 1f)
			{
				if (num6 < 0.5f)
				{
					return i;
				}
				return i + 1;
			}
			num5 = Mathf.Abs(num5);
			if (num5 < num3)
			{
				num2 = i;
				num3 = num5;
				num4 = num6;
			}
		}
		if (num2 == -1)
		{
			return num;
		}
		if (num4 < 0.5f)
		{
			return num2;
		}
		return num2 + 1;
	}

	private int LineDownCharacterPosition(int originalPos, bool goToLastChar)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		if (originalPos >= m_TextComponent.textInfo.characterCount)
		{
			return m_TextComponent.textInfo.characterCount - 1;
		}
		TMP_CharacterInfo val = m_TextComponent.textInfo.characterInfo[originalPos];
		int lineNumber = val.lineNumber;
		if (lineNumber + 1 >= m_TextComponent.textInfo.lineCount)
		{
			if (!goToLastChar)
			{
				return originalPos;
			}
			return m_TextComponent.textInfo.characterCount - 1;
		}
		int lastCharacterIndex = m_TextComponent.textInfo.lineInfo[lineNumber + 1].lastCharacterIndex;
		int num = -1;
		float num2 = 32767f;
		float num3 = 0f;
		for (int i = m_TextComponent.textInfo.lineInfo[lineNumber + 1].firstCharacterIndex; i < lastCharacterIndex; i++)
		{
			TMP_CharacterInfo val2 = m_TextComponent.textInfo.characterInfo[i];
			float num4 = val.origin - val2.origin;
			float num5 = num4 / (val2.xAdvance - val2.origin);
			if (num5 >= 0f && num5 <= 1f)
			{
				if (num5 < 0.5f)
				{
					return i;
				}
				return i + 1;
			}
			num4 = Mathf.Abs(num4);
			if (num4 < num2)
			{
				num = i;
				num2 = num4;
				num3 = num5;
			}
		}
		if (num == -1)
		{
			return lastCharacterIndex;
		}
		if (num3 < 0.5f)
		{
			return num;
		}
		return num + 1;
	}

	private int PageUpCharacterPosition(int originalPos, bool goToFirstChar)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		if (originalPos >= m_TextComponent.textInfo.characterCount)
		{
			originalPos--;
		}
		TMP_CharacterInfo val = m_TextComponent.textInfo.characterInfo[originalPos];
		int lineNumber = val.lineNumber;
		if (lineNumber - 1 < 0)
		{
			if (!goToFirstChar)
			{
				return originalPos;
			}
			return 0;
		}
		float height = m_TextViewport.rect.height;
		int num = lineNumber - 1;
		while (num > 0 && !(m_TextComponent.textInfo.lineInfo[num].baseline > m_TextComponent.textInfo.lineInfo[lineNumber].baseline + height))
		{
			num--;
		}
		int lastCharacterIndex = m_TextComponent.textInfo.lineInfo[num].lastCharacterIndex;
		int num2 = -1;
		float num3 = 32767f;
		float num4 = 0f;
		for (int i = m_TextComponent.textInfo.lineInfo[num].firstCharacterIndex; i < lastCharacterIndex; i++)
		{
			TMP_CharacterInfo val2 = m_TextComponent.textInfo.characterInfo[i];
			float num5 = val.origin - val2.origin;
			float num6 = num5 / (val2.xAdvance - val2.origin);
			if (num6 >= 0f && num6 <= 1f)
			{
				if (num6 < 0.5f)
				{
					return i;
				}
				return i + 1;
			}
			num5 = Mathf.Abs(num5);
			if (num5 < num3)
			{
				num2 = i;
				num3 = num5;
				num4 = num6;
			}
		}
		if (num2 == -1)
		{
			return lastCharacterIndex;
		}
		if (num4 < 0.5f)
		{
			return num2;
		}
		return num2 + 1;
	}

	private int PageDownCharacterPosition(int originalPos, bool goToLastChar)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		if (originalPos >= m_TextComponent.textInfo.characterCount)
		{
			return m_TextComponent.textInfo.characterCount - 1;
		}
		TMP_CharacterInfo val = m_TextComponent.textInfo.characterInfo[originalPos];
		int lineNumber = val.lineNumber;
		if (lineNumber + 1 >= m_TextComponent.textInfo.lineCount)
		{
			if (!goToLastChar)
			{
				return originalPos;
			}
			return m_TextComponent.textInfo.characterCount - 1;
		}
		float height = m_TextViewport.rect.height;
		int i;
		for (i = lineNumber + 1; i < m_TextComponent.textInfo.lineCount - 1 && !(m_TextComponent.textInfo.lineInfo[i].baseline < m_TextComponent.textInfo.lineInfo[lineNumber].baseline - height); i++)
		{
		}
		int lastCharacterIndex = m_TextComponent.textInfo.lineInfo[i].lastCharacterIndex;
		int num = -1;
		float num2 = 32767f;
		float num3 = 0f;
		for (int j = m_TextComponent.textInfo.lineInfo[i].firstCharacterIndex; j < lastCharacterIndex; j++)
		{
			TMP_CharacterInfo val2 = m_TextComponent.textInfo.characterInfo[j];
			float num4 = val.origin - val2.origin;
			float num5 = num4 / (val2.xAdvance - val2.origin);
			if (num5 >= 0f && num5 <= 1f)
			{
				if (num5 < 0.5f)
				{
					return j;
				}
				return j + 1;
			}
			num4 = Mathf.Abs(num4);
			if (num4 < num2)
			{
				num = j;
				num2 = num4;
				num3 = num5;
			}
		}
		if (num == -1)
		{
			return lastCharacterIndex;
		}
		if (num3 < 0.5f)
		{
			return num;
		}
		return num + 1;
	}

	private void MoveDown(bool shift)
	{
		MoveDown(shift, goToLastChar: true);
	}

	private void MoveDown(bool shift, bool goToLastChar)
	{
		if (hasSelection && !shift)
		{
			int num2 = (caretSelectPositionInternal = Mathf.Max(caretPositionInternal, caretSelectPositionInternal));
			caretPositionInternal = num2;
		}
		int num3 = (multiLine ? LineDownCharacterPosition(caretSelectPositionInternal, goToLastChar) : (m_TextComponent.textInfo.characterCount - 1));
		if (shift)
		{
			caretSelectPositionInternal = num3;
			stringSelectPositionInternal = GetStringIndexFromCaretPosition(caretSelectPositionInternal);
		}
		else
		{
			int num2 = (caretPositionInternal = num3);
			caretSelectPositionInternal = num2;
			num2 = (stringPositionInternal = GetStringIndexFromCaretPosition(caretSelectPositionInternal));
			stringSelectPositionInternal = num2;
		}
		SendOnCarretMoved();
	}

	private void MoveUp(bool shift)
	{
		MoveUp(shift, goToFirstChar: true);
	}

	private void MoveUp(bool shift, bool goToFirstChar)
	{
		if (hasSelection && !shift)
		{
			int num2 = (caretSelectPositionInternal = Mathf.Min(caretPositionInternal, caretSelectPositionInternal));
			caretPositionInternal = num2;
		}
		int num3 = (multiLine ? LineUpCharacterPosition(caretSelectPositionInternal, goToFirstChar) : 0);
		if (shift)
		{
			caretSelectPositionInternal = num3;
			stringSelectPositionInternal = GetStringIndexFromCaretPosition(caretSelectPositionInternal);
		}
		else
		{
			int num2 = (caretPositionInternal = num3);
			caretSelectPositionInternal = num2;
			num2 = (stringPositionInternal = GetStringIndexFromCaretPosition(caretSelectPositionInternal));
			stringSelectPositionInternal = num2;
		}
		SendOnCarretMoved();
	}

	private void MovePageUp(bool shift)
	{
		MovePageUp(shift, goToFirstChar: true);
	}

	private void MovePageUp(bool shift, bool goToFirstChar)
	{
		if (hasSelection && !shift)
		{
			int num2 = (caretSelectPositionInternal = Mathf.Min(caretPositionInternal, caretSelectPositionInternal));
			caretPositionInternal = num2;
		}
		int num3 = (multiLine ? PageUpCharacterPosition(caretSelectPositionInternal, goToFirstChar) : 0);
		if (shift)
		{
			caretSelectPositionInternal = num3;
			stringSelectPositionInternal = GetStringIndexFromCaretPosition(caretSelectPositionInternal);
		}
		else
		{
			int num2 = (caretPositionInternal = num3);
			caretSelectPositionInternal = num2;
			num2 = (stringPositionInternal = GetStringIndexFromCaretPosition(caretSelectPositionInternal));
			stringSelectPositionInternal = num2;
		}
		if (m_LineType != 0)
		{
			float height = m_TextViewport.rect.height;
			float num5 = m_TextComponent.rectTransform.position.y + m_TextComponent.textBounds.max.y;
			float num6 = m_TextViewport.position.y + m_TextViewport.rect.yMax;
			height = ((num6 > num5 + height) ? height : (num6 - num5));
			m_TextComponent.rectTransform.anchoredPosition += new Vector2(0f, height);
			AssignPositioningIfNeeded();
		}
		SendOnCarretMoved();
	}

	private void MovePageDown(bool shift)
	{
		MovePageDown(shift, goToLastChar: true);
	}

	private void MovePageDown(bool shift, bool goToLastChar)
	{
		if (hasSelection && !shift)
		{
			int num2 = (caretSelectPositionInternal = Mathf.Max(caretPositionInternal, caretSelectPositionInternal));
			caretPositionInternal = num2;
		}
		int num3 = (multiLine ? PageDownCharacterPosition(caretSelectPositionInternal, goToLastChar) : (m_TextComponent.textInfo.characterCount - 1));
		if (shift)
		{
			caretSelectPositionInternal = num3;
			stringSelectPositionInternal = GetStringIndexFromCaretPosition(caretSelectPositionInternal);
		}
		else
		{
			int num2 = (caretPositionInternal = num3);
			caretSelectPositionInternal = num2;
			num2 = (stringPositionInternal = GetStringIndexFromCaretPosition(caretSelectPositionInternal));
			stringSelectPositionInternal = num2;
		}
		if (m_LineType != 0)
		{
			float height = m_TextViewport.rect.height;
			float num5 = m_TextComponent.rectTransform.position.y + m_TextComponent.textBounds.min.y;
			float num6 = m_TextViewport.position.y + m_TextViewport.rect.yMin;
			height = ((num6 > num5 + height) ? height : (num6 - num5));
			m_TextComponent.rectTransform.anchoredPosition += new Vector2(0f, height);
			AssignPositioningIfNeeded();
		}
		SendOnCarretMoved();
	}

	private void Delete()
	{
		if (m_ReadOnly || m_StringPosition == m_StringSelectPosition)
		{
			return;
		}
		if (m_isRichTextEditingAllowed || m_isSelectAll)
		{
			if (m_StringPosition < m_StringSelectPosition)
			{
				m_Text = text.Remove(m_StringPosition, m_StringSelectPosition - m_StringPosition);
				m_StringSelectPosition = m_StringPosition;
			}
			else
			{
				m_Text = text.Remove(m_StringSelectPosition, m_StringPosition - m_StringSelectPosition);
				m_StringPosition = m_StringSelectPosition;
			}
			if (m_isSelectAll)
			{
				m_CaretPosition = (m_CaretSelectPosition = 0);
				m_isSelectAll = false;
			}
		}
		else if (m_CaretPosition < m_CaretSelectPosition)
		{
			int num = ClampArrayIndex(m_CaretSelectPosition - 1);
			m_StringPosition = m_TextComponent.textInfo.characterInfo[m_CaretPosition].index;
			m_StringSelectPosition = m_TextComponent.textInfo.characterInfo[num].index + m_TextComponent.textInfo.characterInfo[num].stringLength;
			m_Text = text.Remove(m_StringPosition, m_StringSelectPosition - m_StringPosition);
			m_StringSelectPosition = m_StringPosition;
			m_CaretSelectPosition = m_CaretPosition;
		}
		else
		{
			int num2 = ClampArrayIndex(m_CaretPosition - 1);
			m_StringPosition = m_TextComponent.textInfo.characterInfo[num2].index + m_TextComponent.textInfo.characterInfo[num2].stringLength;
			m_StringSelectPosition = m_TextComponent.textInfo.characterInfo[m_CaretSelectPosition].index;
			m_Text = text.Remove(m_StringSelectPosition, m_StringPosition - m_StringSelectPosition);
			m_StringPosition = m_StringSelectPosition;
			m_CaretPosition = m_CaretSelectPosition;
		}
		SendOnCarretMoved();
	}

	private void DeleteKey()
	{
		if (m_ReadOnly)
		{
			return;
		}
		if (hasSelection)
		{
			m_HasTextBeenRemoved = true;
			Delete();
			UpdateTouchKeyboardFromEditChanges();
			SendOnValueChangedAndUpdateLabel();
		}
		else if (m_isRichTextEditingAllowed)
		{
			if (stringPositionInternal < text.Length)
			{
				if (char.IsHighSurrogate(text[stringPositionInternal]))
				{
					m_Text = text.Remove(stringPositionInternal, 2);
				}
				else
				{
					m_Text = text.Remove(stringPositionInternal, 1);
				}
				m_HasTextBeenRemoved = true;
				UpdateTouchKeyboardFromEditChanges();
				SendOnValueChangedAndUpdateLabel();
			}
		}
		else if (caretPositionInternal < m_TextComponent.textInfo.characterCount - 1)
		{
			int num = m_TextComponent.textInfo.characterInfo[caretPositionInternal].stringLength;
			if (m_TextComponent.textInfo.characterInfo[caretPositionInternal].character == '\r' && m_TextComponent.textInfo.characterInfo[caretPositionInternal + 1].character == '\n')
			{
				num += m_TextComponent.textInfo.characterInfo[caretPositionInternal + 1].stringLength;
			}
			int index = m_TextComponent.textInfo.characterInfo[caretPositionInternal].index;
			m_Text = text.Remove(index, num);
			m_HasTextBeenRemoved = true;
			SendOnValueChangedAndUpdateLabel();
		}
	}

	private void Backspace()
	{
		if (m_ReadOnly)
		{
			return;
		}
		if (hasSelection)
		{
			m_HasTextBeenRemoved = true;
			Delete();
			UpdateTouchKeyboardFromEditChanges();
			SendOnValueChangedAndUpdateLabel();
		}
		else if (m_isRichTextEditingAllowed)
		{
			if (stringPositionInternal > 0)
			{
				int num = 1;
				if (char.IsLowSurrogate(text[stringPositionInternal - 1]))
				{
					num = 2;
				}
				stringSelectPositionInternal = (stringPositionInternal -= num);
				m_Text = text.Remove(stringPositionInternal, num);
				caretSelectPositionInternal = --caretPositionInternal;
				m_HasTextBeenRemoved = true;
				UpdateTouchKeyboardFromEditChanges();
				SendOnValueChangedAndUpdateLabel();
			}
		}
		else
		{
			if (caretPositionInternal > 0)
			{
				int num2 = caretPositionInternal - 1;
				int num3 = m_TextComponent.textInfo.characterInfo[num2].stringLength;
				if (num2 > 0 && m_TextComponent.textInfo.characterInfo[num2].character == '\n' && m_TextComponent.textInfo.characterInfo[num2 - 1].character == '\r')
				{
					num3 += m_TextComponent.textInfo.characterInfo[num2 - 1].stringLength;
					num2--;
				}
				m_Text = text.Remove(m_TextComponent.textInfo.characterInfo[num2].index, num3);
				int num5 = (stringPositionInternal = ((caretPositionInternal < 1) ? m_TextComponent.textInfo.characterInfo[0].index : m_TextComponent.textInfo.characterInfo[num2].index));
				stringSelectPositionInternal = num5;
				num5 = (caretPositionInternal = num2);
				caretSelectPositionInternal = num5;
			}
			m_HasTextBeenRemoved = true;
			UpdateTouchKeyboardFromEditChanges();
			SendOnValueChangedAndUpdateLabel();
		}
		SendOnBackSpace();
	}

	protected virtual void Append(string input)
	{
		if (m_ReadOnly || !InPlaceEditing())
		{
			return;
		}
		string text = "";
		int i = 0;
		for (int length = input.Length; i < length; i++)
		{
			char c = input[i];
			if (c >= ' ' || c == '\t' || c == '\r' || c == '\n' || c == '\n')
			{
				Append(c);
				text += c;
			}
		}
		SendOnTextInserted(text);
	}

	protected virtual void Append(char input)
	{
		if (m_ReadOnly || !InPlaceEditing())
		{
			return;
		}
		int num = Mathf.Min(stringPositionInternal, stringSelectPositionInternal);
		string text = this.text;
		if (selectionFocusPosition != selectionAnchorPosition)
		{
			m_HasTextBeenRemoved = true;
			if (m_isRichTextEditingAllowed || m_isSelectAll)
			{
				text = ((m_StringPosition >= m_StringSelectPosition) ? this.text.Remove(m_StringSelectPosition, m_StringPosition - m_StringSelectPosition) : this.text.Remove(m_StringPosition, m_StringSelectPosition - m_StringPosition));
			}
			else if (m_CaretPosition < m_CaretSelectPosition)
			{
				m_StringPosition = m_TextComponent.textInfo.characterInfo[m_CaretPosition].index;
				m_StringSelectPosition = m_TextComponent.textInfo.characterInfo[m_CaretSelectPosition - 1].index + m_TextComponent.textInfo.characterInfo[m_CaretSelectPosition - 1].stringLength;
				text = this.text.Remove(m_StringPosition, m_StringSelectPosition - m_StringPosition);
			}
			else
			{
				m_StringPosition = m_TextComponent.textInfo.characterInfo[m_CaretPosition - 1].index + m_TextComponent.textInfo.characterInfo[m_CaretPosition - 1].stringLength;
				m_StringSelectPosition = m_TextComponent.textInfo.characterInfo[m_CaretSelectPosition].index;
				text = this.text.Remove(m_StringSelectPosition, m_StringPosition - m_StringSelectPosition);
			}
		}
		if (onValidateInput != null)
		{
			input = onValidateInput(text, num, input);
		}
		else
		{
			if (characterValidation == CharacterValidation.CustomValidator)
			{
				input = Validate(text, num, input);
				if (input != 0)
				{
					if (!char.IsHighSurrogate(input))
					{
						m_CaretSelectPosition = ++m_CaretPosition;
					}
					SendOnValueChanged();
					UpdateLabel();
				}
				return;
			}
			if (characterValidation != 0)
			{
				input = Validate(text, num, input);
			}
		}
		if (input != 0)
		{
			Insert(input);
		}
	}

	private void Insert(char c)
	{
		if (m_ReadOnly)
		{
			return;
		}
		string value = c.ToString();
		Delete();
		if (characterLimit <= 0 || text.Length < characterLimit)
		{
			m_Text = text.Insert(m_StringPosition, value);
			if (!char.IsHighSurrogate(c))
			{
				m_CaretSelectPosition = ++m_CaretPosition;
			}
			m_StringSelectPosition = ++m_StringPosition;
			UpdateTouchKeyboardFromEditChanges();
			SendOnValueChanged();
		}
	}

	private void UpdateTouchKeyboardFromEditChanges()
	{
		if (m_SoftKeyboard != null && InPlaceEditing())
		{
			m_SoftKeyboard.text = m_Text;
		}
	}

	private void SendOnValueChangedAndUpdateLabel()
	{
		UpdateLabel();
		SendOnValueChanged();
	}

	private void SendOnValueChanged()
	{
		if (onValueChanged != null)
		{
			onValueChanged.Invoke(text);
		}
	}

	private void SendOnTextInserted(string s)
	{
		if (onTextInserted != null)
		{
			onTextInserted.Invoke(s);
		}
	}

	private void SendOnTextMeshUpdated()
	{
		if (onTextMeshUpdated != null)
		{
			onTextMeshUpdated.Invoke();
		}
	}

	private void SendOnCarretMoved()
	{
		if (onCarretMoved != null)
		{
			onCarretMoved.Invoke();
		}
	}

	private void SendOnBackSpace()
	{
		if (onBackSpace != null)
		{
			onBackSpace.Invoke();
		}
	}

	protected void SendOnEndEdit()
	{
		if (onEndEdit != null)
		{
			onEndEdit.Invoke(m_Text);
		}
	}

	protected void SendOnSubmit()
	{
		if (onSubmit != null)
		{
			onSubmit.Invoke(m_Text);
		}
	}

	protected void SendOnFocus()
	{
		if (onSelect != null)
		{
			onSelect.Invoke(m_Text);
		}
	}

	protected void SendOnFocusLost()
	{
		if (onDeselect != null)
		{
			onDeselect.Invoke(m_Text);
		}
		SendOnCarretMoved();
	}

	protected void SendOnTextSelection()
	{
		m_isSelected = true;
		if (onTextSelection != null)
		{
			onTextSelection.Invoke(m_Text, stringPositionInternal, stringSelectPositionInternal);
		}
	}

	protected void SendOnEndTextSelection()
	{
		if (m_isSelected)
		{
			if (onEndTextSelection != null)
			{
				onEndTextSelection.Invoke(m_Text, stringPositionInternal, stringSelectPositionInternal);
			}
			m_isSelected = false;
		}
	}

	protected void SendTouchScreenKeyboardStatusChanged()
	{
		if (m_SoftKeyboard != null && onTouchScreenKeyboardStatusChanged != null)
		{
			onTouchScreenKeyboardStatusChanged.Invoke(m_SoftKeyboard.status);
		}
	}

	protected void UpdateLabel()
	{
		if (!((UnityEngine.Object)(object)m_TextComponent != null) || !((UnityEngine.Object)(object)m_TextComponent.font != null) || m_PreventCallback)
		{
			return;
		}
		m_PreventCallback = true;
		string text;
		if (compositionLength > 0 && !m_ReadOnly)
		{
			Delete();
			text = ((!m_RichText) ? (this.text.Substring(0, m_StringPosition) + compositionString + this.text.Substring(m_StringPosition)) : (this.text.Substring(0, m_StringPosition) + "<u>" + compositionString + "</u>" + this.text.Substring(m_StringPosition)));
			m_IsCompositionActive = true;
		}
		else
		{
			text = this.text;
			m_IsCompositionActive = false;
			m_ShouldUpdateIMEWindowPosition = true;
		}
		string text2 = ((inputType != InputType.Password) ? text : new string(asteriskChar, text.Length));
		bool flag = string.IsNullOrEmpty(text);
		if (m_Placeholder != null)
		{
			m_Placeholder.enabled = flag;
		}
		if (!flag && !m_ReadOnly)
		{
			SetCaretVisible();
		}
		m_TextComponent.text = text2 + "\u200b";
		if (m_IsDrivenByLayoutComponents)
		{
			LayoutRebuilder.MarkLayoutForRebuild(m_RectTransform);
		}
		if (m_LineLimit > 0)
		{
			m_TextComponent.ForceMeshUpdate(false, false);
			TMP_TextInfo textInfo = m_TextComponent.textInfo;
			if (textInfo != null && textInfo.lineCount > m_LineLimit)
			{
				int lastCharacterIndex = textInfo.lineInfo[m_LineLimit - 1].lastCharacterIndex;
				int num = textInfo.characterInfo[lastCharacterIndex].index + textInfo.characterInfo[lastCharacterIndex].stringLength;
				this.text = text2.Remove(num, text2.Length - num);
				m_TextComponent.text = this.text + "\u200b";
			}
		}
		if (m_IsTextComponentUpdateRequired || ((bool)m_VerticalScrollbar && (!m_IsCaretPositionDirty || !m_IsStringPositionDirty)))
		{
			m_IsTextComponentUpdateRequired = false;
			m_TextComponent.ForceMeshUpdate(false, false);
			SendOnTextMeshUpdated();
		}
		MarkGeometryAsDirty();
		m_PreventCallback = false;
	}

	private void UpdateScrollbar()
	{
		if ((bool)m_VerticalScrollbar)
		{
			float size = m_TextViewport.rect.height / m_TextComponent.preferredHeight;
			m_VerticalScrollbar.size = size;
			m_VerticalScrollbar.value = GetScrollPositionRelativeToViewport();
		}
	}

	private void OnScrollbarValueChange(float value)
	{
		if (!(value < 0f) && !(value > 1f))
		{
			AdjustTextPositionRelativeToViewport(value);
			m_ScrollPosition = value;
		}
	}

	private void UpdateMaskRegions()
	{
	}

	private void AdjustTextPositionRelativeToViewport(float relativePosition)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Invalid comparison between Unknown and I4
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Invalid comparison between Unknown and I4
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Invalid comparison between Unknown and I4
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Invalid comparison between Unknown and I4
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Invalid comparison between Unknown and I4
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Invalid comparison between Unknown and I4
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Invalid comparison between Unknown and I4
		if (m_TextViewport == null)
		{
			return;
		}
		TMP_TextInfo textInfo = m_TextComponent.textInfo;
		if (textInfo == null || textInfo.lineInfo == null || textInfo.lineCount == 0 || textInfo.lineCount > textInfo.lineInfo.Length)
		{
			return;
		}
		float num = 0f;
		float y = m_TextComponent.preferredHeight;
		VerticalAlignmentOptions verticalAlignment = m_TextComponent.verticalAlignment;
		if ((int)verticalAlignment <= 1024)
		{
			if ((int)verticalAlignment != 256)
			{
				if ((int)verticalAlignment != 512)
				{
					if ((int)verticalAlignment == 1024)
					{
						num = 1f;
					}
				}
				else
				{
					num = 0.5f;
				}
			}
			else
			{
				num = 0f;
			}
		}
		else if ((int)verticalAlignment != 2048)
		{
			if ((int)verticalAlignment != 4096)
			{
				if ((int)verticalAlignment == 8192)
				{
					num = 0.5f;
				}
			}
			else
			{
				num = 0.5f;
				y = m_TextComponent.bounds.size.y;
			}
		}
		m_TextComponent.rectTransform.anchoredPosition = new Vector2(m_TextComponent.rectTransform.anchoredPosition.x, (y - m_TextViewport.rect.height) * (relativePosition - num));
		AssignPositioningIfNeeded();
	}

	private int GetCaretPositionFromStringIndex(int stringIndex)
	{
		int characterCount = m_TextComponent.textInfo.characterCount;
		for (int i = 0; i < characterCount; i++)
		{
			if (m_TextComponent.textInfo.characterInfo[i].index >= stringIndex)
			{
				return i;
			}
		}
		return characterCount;
	}

	private int GetMinCaretPositionFromStringIndex(int stringIndex)
	{
		int characterCount = m_TextComponent.textInfo.characterCount;
		for (int i = 0; i < characterCount; i++)
		{
			if (stringIndex < m_TextComponent.textInfo.characterInfo[i].index + m_TextComponent.textInfo.characterInfo[i].stringLength)
			{
				return i;
			}
		}
		return characterCount;
	}

	private int GetMaxCaretPositionFromStringIndex(int stringIndex)
	{
		int characterCount = m_TextComponent.textInfo.characterCount;
		for (int i = 0; i < characterCount; i++)
		{
			if (m_TextComponent.textInfo.characterInfo[i].index >= stringIndex)
			{
				return i;
			}
		}
		return characterCount;
	}

	private int GetStringIndexFromCaretPosition(int caretPosition)
	{
		ClampCaretPos(ref caretPosition);
		return m_TextComponent.textInfo.characterInfo[caretPosition].index;
	}

	private void UpdateStringIndexFromCaretPosition()
	{
		stringPositionInternal = GetStringIndexFromCaretPosition(m_CaretPosition);
		stringSelectPositionInternal = GetStringIndexFromCaretPosition(m_CaretSelectPosition);
		m_IsStringPositionDirty = false;
	}

	private void UpdateCaretPositionFromStringIndex()
	{
		caretPositionInternal = GetCaretPositionFromStringIndex(stringPositionInternal);
		caretSelectPositionInternal = GetCaretPositionFromStringIndex(stringSelectPositionInternal);
		m_IsCaretPositionDirty = false;
	}

	public void ForceLabelUpdate()
	{
		UpdateLabel();
	}

	private void MarkGeometryAsDirty()
	{
		CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
	}

	public virtual void Rebuild(CanvasUpdate update)
	{
		if (update == CanvasUpdate.LatePreRender)
		{
			UpdateGeometry();
		}
	}

	public virtual void LayoutComplete()
	{
	}

	public virtual void GraphicUpdateComplete()
	{
	}

	private void UpdateGeometry()
	{
		if ((InPlaceEditing() || isUWP()) && !((UnityEngine.Object)(object)m_CachedInputRenderer == null))
		{
			OnFillVBO(mesh);
			m_CachedInputRenderer.SetMesh(mesh);
		}
	}

	private void AssignPositioningIfNeeded()
	{
		if ((UnityEngine.Object)(object)m_TextComponent != null && caretRectTrans != null && (caretRectTrans.localPosition != m_TextComponent.rectTransform.localPosition || caretRectTrans.localRotation != m_TextComponent.rectTransform.localRotation || caretRectTrans.localScale != m_TextComponent.rectTransform.localScale || caretRectTrans.anchorMin != m_TextComponent.rectTransform.anchorMin || caretRectTrans.anchorMax != m_TextComponent.rectTransform.anchorMax || caretRectTrans.anchoredPosition != m_TextComponent.rectTransform.anchoredPosition || caretRectTrans.sizeDelta != m_TextComponent.rectTransform.sizeDelta || caretRectTrans.pivot != m_TextComponent.rectTransform.pivot))
		{
			caretRectTrans.localPosition = m_TextComponent.rectTransform.localPosition;
			caretRectTrans.localRotation = m_TextComponent.rectTransform.localRotation;
			caretRectTrans.localScale = m_TextComponent.rectTransform.localScale;
			caretRectTrans.anchorMin = m_TextComponent.rectTransform.anchorMin;
			caretRectTrans.anchorMax = m_TextComponent.rectTransform.anchorMax;
			caretRectTrans.anchoredPosition = m_TextComponent.rectTransform.anchoredPosition;
			caretRectTrans.sizeDelta = m_TextComponent.rectTransform.sizeDelta;
			caretRectTrans.pivot = m_TextComponent.rectTransform.pivot;
		}
	}

	private void OnFillVBO(Mesh vbo)
	{
		using VertexHelper vertexHelper = new VertexHelper();
		if (!isFocused && !m_SelectionStillActive)
		{
			vertexHelper.FillMesh(vbo);
			return;
		}
		if (m_IsStringPositionDirty)
		{
			UpdateStringIndexFromCaretPosition();
		}
		if (m_IsCaretPositionDirty)
		{
			UpdateCaretPositionFromStringIndex();
		}
		if (!hasSelection)
		{
			GenerateCaret(vertexHelper, Vector2.zero);
			SendOnEndTextSelection();
		}
		else
		{
			GenerateHighlight(vertexHelper, Vector2.zero);
			SendOnTextSelection();
		}
		vertexHelper.FillMesh(vbo);
	}

	private void GenerateCaret(VertexHelper vbo, Vector2 roundingOffset)
	{
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Invalid comparison between Unknown and I4
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Invalid comparison between Unknown and I4
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_0383: Unknown result type (might be due to invalid IL or missing references)
		if (!m_CaretVisible || (UnityEngine.Object)(object)((Graphic)(object)m_TextComponent).canvas == null || m_ReadOnly)
		{
			return;
		}
		if (m_CursorVerts == null)
		{
			CreateCursorVerts();
		}
		float num = m_CaretWidth;
		Vector2 zero = Vector2.zero;
		float num2 = 0f;
		if (caretPositionInternal >= m_TextComponent.textInfo.characterInfo.Length || caretPositionInternal < 0)
		{
			return;
		}
		int lineNumber = m_TextComponent.textInfo.characterInfo[caretPositionInternal].lineNumber;
		TMP_CharacterInfo val;
		if (caretPositionInternal == m_TextComponent.textInfo.lineInfo[lineNumber].firstCharacterIndex)
		{
			val = m_TextComponent.textInfo.characterInfo[caretPositionInternal];
			num2 = val.ascender - val.descender;
			zero = (((int)m_TextComponent.verticalAlignment != 4096) ? new Vector2(val.origin, val.descender) : new Vector2(val.origin, 0f - num2 / 2f));
		}
		else
		{
			val = m_TextComponent.textInfo.characterInfo[caretPositionInternal - 1];
			num2 = val.ascender - val.descender;
			zero = (((int)m_TextComponent.verticalAlignment != 4096) ? new Vector2(val.xAdvance, val.descender) : new Vector2(val.xAdvance, 0f - num2 / 2f));
		}
		if (m_SoftKeyboard != null && compositionLength == 0)
		{
			int num3 = m_StringPosition;
			int num4 = ((m_SoftKeyboard.text != null) ? m_SoftKeyboard.text.Length : 0);
			if (num3 < 0)
			{
				num3 = 0;
			}
			if (num3 > num4)
			{
				num3 = num4;
			}
			m_SoftKeyboard.selection = new RangeInt(num3, 0);
		}
		if ((isFocused && zero != m_LastPosition) || m_forceRectTransformAdjustment || m_HasTextBeenRemoved)
		{
			AdjustRectTransformRelativeToViewport(zero, num2, val.isVisible);
		}
		m_LastPosition = zero;
		float num5 = zero.y + num2;
		float y = num5 - num2;
		_ = ((Graphic)(object)m_TextComponent).canvas.scaleFactor;
		m_CursorVerts[0].position = new Vector3(zero.x, y, 0f);
		m_CursorVerts[1].position = new Vector3(zero.x, num5, 0f);
		m_CursorVerts[2].position = new Vector3(zero.x + num, num5, 0f);
		m_CursorVerts[3].position = new Vector3(zero.x + num, y, 0f);
		m_CursorVerts[0].color = caretColor;
		m_CursorVerts[1].color = caretColor;
		m_CursorVerts[2].color = caretColor;
		m_CursorVerts[3].color = caretColor;
		vbo.AddUIVertexQuad(m_CursorVerts);
		if (!m_ShouldUpdateIMEWindowPosition && lineNumber == m_PreviousIMEInsertionLine)
		{
			return;
		}
		m_ShouldUpdateIMEWindowPosition = false;
		m_PreviousIMEInsertionLine = lineNumber;
		Camera camera;
		if ((int)((Graphic)(object)m_TextComponent).canvas.renderMode == 0)
		{
			camera = null;
		}
		else
		{
			camera = ((Graphic)(object)m_TextComponent).canvas.worldCamera;
			if (camera == null)
			{
				camera = Camera.current;
			}
		}
		Vector3 vector = ((Component)(object)m_CachedInputRenderer).gameObject.transform.TransformPoint(m_CursorVerts[0].position);
		Vector2 compositionCursorPos = RectTransformUtility.WorldToScreenPoint(camera, vector);
		compositionCursorPos.y = (float)Screen.height - compositionCursorPos.y;
		if (inputSystem != null)
		{
			inputSystem.compositionCursorPos = compositionCursorPos;
		}
	}

	private void CreateCursorVerts()
	{
		m_CursorVerts = new UIVertex[4];
		for (int i = 0; i < m_CursorVerts.Length; i++)
		{
			m_CursorVerts[i] = UIVertex.simpleVert;
			m_CursorVerts[i].uv0 = Vector2.zero;
		}
	}

	private void GenerateHighlight(VertexHelper vbo, Vector2 roundingOffset)
	{
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		UpdateMaskRegions();
		TMP_TextInfo textInfo = m_TextComponent.textInfo;
		if (textInfo.characterCount == 0)
		{
			return;
		}
		m_CaretPosition = GetCaretPositionFromStringIndex(stringPositionInternal);
		m_CaretSelectPosition = GetCaretPositionFromStringIndex(stringSelectPositionInternal);
		if (m_SoftKeyboard != null && compositionLength == 0)
		{
			int num = ((m_CaretPosition < m_CaretSelectPosition) ? textInfo.characterInfo[m_CaretPosition].index : textInfo.characterInfo[m_CaretSelectPosition].index);
			int length = ((m_CaretPosition < m_CaretSelectPosition) ? (stringSelectPositionInternal - num) : (stringPositionInternal - num));
			m_SoftKeyboard.selection = new RangeInt(num, length);
		}
		float num2 = 0f;
		Vector2 startPosition;
		if (m_CaretSelectPosition < textInfo.characterCount)
		{
			startPosition = new Vector2(textInfo.characterInfo[m_CaretSelectPosition].origin, textInfo.characterInfo[m_CaretSelectPosition].descender);
			num2 = textInfo.characterInfo[m_CaretSelectPosition].ascender - textInfo.characterInfo[m_CaretSelectPosition].descender;
		}
		else
		{
			startPosition = new Vector2(textInfo.characterInfo[m_CaretSelectPosition - 1].xAdvance, textInfo.characterInfo[m_CaretSelectPosition - 1].descender);
			num2 = textInfo.characterInfo[m_CaretSelectPosition - 1].ascender - textInfo.characterInfo[m_CaretSelectPosition - 1].descender;
		}
		AdjustRectTransformRelativeToViewport(startPosition, num2, isCharVisible: true);
		int num3 = Mathf.Max(0, m_CaretPosition);
		int num4 = Mathf.Max(0, m_CaretSelectPosition);
		if (num3 > num4)
		{
			int num5 = num3;
			num3 = num4;
			num4 = num5;
		}
		num4--;
		int num6 = textInfo.characterInfo[num3].lineNumber;
		int lastCharacterIndex = textInfo.lineInfo[num6].lastCharacterIndex;
		UIVertex simpleVert = UIVertex.simpleVert;
		simpleVert.uv0 = Vector2.zero;
		simpleVert.color = selectionColor;
		for (int i = num3; i <= num4 && i < textInfo.characterCount; i++)
		{
			if (i == lastCharacterIndex || i == num4)
			{
				TMP_CharacterInfo val = textInfo.characterInfo[num3];
				TMP_CharacterInfo val2 = textInfo.characterInfo[i];
				if (i > 0 && val2.character == '\n' && textInfo.characterInfo[i - 1].character == '\r')
				{
					val2 = textInfo.characterInfo[i - 1];
				}
				Vector2 vector = new Vector2(val.origin, textInfo.lineInfo[num6].ascender);
				Vector2 vector2 = new Vector2(val2.xAdvance, textInfo.lineInfo[num6].descender);
				int currentVertCount = vbo.currentVertCount;
				simpleVert.position = new Vector3(vector.x, vector2.y, 0f);
				vbo.AddVert(simpleVert);
				simpleVert.position = new Vector3(vector2.x, vector2.y, 0f);
				vbo.AddVert(simpleVert);
				simpleVert.position = new Vector3(vector2.x, vector.y, 0f);
				vbo.AddVert(simpleVert);
				simpleVert.position = new Vector3(vector.x, vector.y, 0f);
				vbo.AddVert(simpleVert);
				vbo.AddTriangle(currentVertCount, currentVertCount + 1, currentVertCount + 2);
				vbo.AddTriangle(currentVertCount + 2, currentVertCount + 3, currentVertCount);
				num3 = i + 1;
				num6++;
				if (num6 < textInfo.lineCount)
				{
					lastCharacterIndex = textInfo.lineInfo[num6].lastCharacterIndex;
				}
			}
		}
	}

	private void AdjustRectTransformRelativeToViewport(Vector2 startPosition, float height, bool isCharVisible)
	{
		if (m_TextViewport == null)
		{
			return;
		}
		Vector3 localPosition = base.transform.localPosition;
		Vector3 localPosition2 = m_TextComponent.rectTransform.localPosition;
		Vector3 localPosition3 = m_TextViewport.localPosition;
		Rect rect = m_TextViewport.rect;
		Vector2 vector = new Vector2(startPosition.x + localPosition2.x + localPosition3.x + localPosition.x, startPosition.y + localPosition2.y + localPosition3.y + localPosition.y);
		Rect rect2 = new Rect(localPosition.x + localPosition3.x + rect.x, localPosition.y + localPosition3.y + rect.y, rect.width, rect.height);
		float num = rect2.xMax - (vector.x + m_TextComponent.margin.z + (float)m_CaretWidth);
		if (num < 0f && (!multiLine || (multiLine && isCharVisible)))
		{
			m_TextComponent.rectTransform.anchoredPosition += new Vector2(num, 0f);
			AssignPositioningIfNeeded();
		}
		float num2 = vector.x - m_TextComponent.margin.x - rect2.xMin;
		if (num2 < 0f)
		{
			m_TextComponent.rectTransform.anchoredPosition += new Vector2(0f - num2, 0f);
			AssignPositioningIfNeeded();
		}
		if (m_LineType != 0)
		{
			float num3 = rect2.yMax - (vector.y + height);
			if (num3 < -0.0001f)
			{
				m_TextComponent.rectTransform.anchoredPosition += new Vector2(0f, num3);
				AssignPositioningIfNeeded();
			}
			float num4 = vector.y - rect2.yMin;
			if (num4 < 0f)
			{
				m_TextComponent.rectTransform.anchoredPosition -= new Vector2(0f, num4);
				AssignPositioningIfNeeded();
			}
		}
		if (m_HasTextBeenRemoved)
		{
			float x = m_TextComponent.rectTransform.anchoredPosition.x;
			float num5 = localPosition.x + localPosition3.x + localPosition2.x + m_TextComponent.textInfo.characterInfo[0].origin - m_TextComponent.margin.x;
			int num6 = ClampArrayIndex(m_TextComponent.textInfo.characterCount - 1);
			float num7 = localPosition.x + localPosition3.x + localPosition2.x + m_TextComponent.textInfo.characterInfo[num6].origin + m_TextComponent.margin.z + (float)m_CaretWidth;
			if (x > 0.0001f && num5 > rect2.xMin)
			{
				float num8 = rect2.xMin - num5;
				if (x < 0f - num8)
				{
					num8 = 0f - x;
				}
				m_TextComponent.rectTransform.anchoredPosition += new Vector2(num8, 0f);
				AssignPositioningIfNeeded();
			}
			else if (x < -0.0001f && num7 < rect2.xMax)
			{
				float num9 = rect2.xMax - num7;
				if (0f - x < num9)
				{
					num9 = 0f - x;
				}
				m_TextComponent.rectTransform.anchoredPosition += new Vector2(num9, 0f);
				AssignPositioningIfNeeded();
			}
			m_HasTextBeenRemoved = false;
		}
		m_forceRectTransformAdjustment = false;
	}

	protected char Validate(string text, int pos, char ch)
	{
		if (characterValidation == CharacterValidation.None || !base.enabled)
		{
			return ch;
		}
		if (characterValidation == CharacterValidation.Integer || characterValidation == CharacterValidation.Decimal)
		{
			bool num = pos == 0 && text.Length > 0 && text[0] == '-';
			bool flag = stringPositionInternal == 0 || stringSelectPositionInternal == 0;
			if (!num)
			{
				if (ch >= '0' && ch <= '9')
				{
					return ch;
				}
				if (ch == '-' && (pos == 0 || flag))
				{
					return ch;
				}
				string numberDecimalSeparator = Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;
				if (ch == Convert.ToChar(numberDecimalSeparator) && characterValidation == CharacterValidation.Decimal && !text.Contains(numberDecimalSeparator))
				{
					return ch;
				}
			}
		}
		else if (characterValidation == CharacterValidation.Digit)
		{
			if (ch >= '0' && ch <= '9')
			{
				return ch;
			}
		}
		else if (characterValidation == CharacterValidation.Alphanumeric)
		{
			if (ch >= 'A' && ch <= 'Z')
			{
				return ch;
			}
			if (ch >= 'a' && ch <= 'z')
			{
				return ch;
			}
			if (ch >= '0' && ch <= '9')
			{
				return ch;
			}
		}
		else if (characterValidation == CharacterValidation.Name)
		{
			char c = ((text.Length > 0) ? text[Mathf.Clamp(pos - 1, 0, text.Length - 1)] : ' ');
			char c2 = ((text.Length > 0) ? text[Mathf.Clamp(pos, 0, text.Length - 1)] : ' ');
			char c3 = ((text.Length > 0) ? text[Mathf.Clamp(pos + 1, 0, text.Length - 1)] : '\n');
			if (char.IsLetter(ch))
			{
				if (char.IsLower(ch) && pos == 0)
				{
					return char.ToUpper(ch);
				}
				if (char.IsLower(ch) && (c == ' ' || c == '-'))
				{
					return char.ToUpper(ch);
				}
				if (char.IsUpper(ch) && pos > 0 && c != ' ' && c != '\'' && c != '-' && !char.IsLower(c))
				{
					return char.ToLower(ch);
				}
				if (char.IsUpper(ch) && char.IsUpper(c2))
				{
					return '\0';
				}
				return ch;
			}
			if (ch == '\'' && c2 != ' ' && c2 != '\'' && c3 != '\'' && !text.Contains("'"))
			{
				return ch;
			}
			if (char.IsLetter(c) && ch == '-' && c2 != '-')
			{
				return ch;
			}
			if ((ch == ' ' || ch == '-') && pos != 0 && c != ' ' && c != '\'' && c != '-' && c2 != ' ' && c2 != '\'' && c2 != '-' && c3 != ' ' && c3 != '\'' && c3 != '-')
			{
				return ch;
			}
		}
		else if (characterValidation == CharacterValidation.EmailAddress)
		{
			if (ch >= 'A' && ch <= 'Z')
			{
				return ch;
			}
			if (ch >= 'a' && ch <= 'z')
			{
				return ch;
			}
			if (ch >= '0' && ch <= '9')
			{
				return ch;
			}
			if (ch == '@' && text.IndexOf('@') == -1)
			{
				return ch;
			}
			if ("!#$%&'*+-/=?^_`{|}~".IndexOf(ch) != -1)
			{
				return ch;
			}
			if (ch == '.')
			{
				char num2 = ((text.Length > 0) ? text[Mathf.Clamp(pos, 0, text.Length - 1)] : ' ');
				char c4 = ((text.Length > 0) ? text[Mathf.Clamp(pos + 1, 0, text.Length - 1)] : '\n');
				if (num2 != '.' && c4 != '.')
				{
					return ch;
				}
			}
		}
		else if (characterValidation == CharacterValidation.Regex)
		{
			if (Regex.IsMatch(ch.ToString(), m_RegexValue))
			{
				return ch;
			}
		}
		else if (characterValidation == CharacterValidation.CustomValidator && (UnityEngine.Object)(object)m_InputValidator != null)
		{
			char result = m_InputValidator.Validate(ref text, ref pos, ch);
			m_Text = text;
			int num4 = (stringPositionInternal = pos);
			stringSelectPositionInternal = num4;
			return result;
		}
		return '\0';
	}

	public void ActivateInputField()
	{
		if (!((UnityEngine.Object)(object)m_TextComponent == null) && !((UnityEngine.Object)(object)m_TextComponent.font == null) && IsActive() && IsInteractable())
		{
			if (isFocused && m_SoftKeyboard != null && !m_SoftKeyboard.active)
			{
				m_SoftKeyboard.active = true;
				m_SoftKeyboard.text = m_Text;
			}
			m_ShouldActivateNextUpdate = true;
		}
	}

	private void ActivateInputFieldInternal()
	{
		if (EventSystem.current == null)
		{
			return;
		}
		if (EventSystem.current.currentSelectedGameObject != base.gameObject)
		{
			EventSystem.current.SetSelectedGameObject(base.gameObject);
		}
		m_TouchKeyboardAllowsInPlaceEditing = !s_IsQuestDevice && TouchScreenKeyboard.isInPlaceEditingAllowed;
		if (TouchScreenKeyboardShouldBeUsed() && !shouldHideSoftKeyboard)
		{
			if (inputSystem != null && inputSystem.touchSupported)
			{
				TouchScreenKeyboard.hideInput = shouldHideMobileInput;
			}
			if (!shouldHideSoftKeyboard && !m_ReadOnly)
			{
				m_SoftKeyboard = ((inputType == InputType.Password) ? TouchScreenKeyboard.Open(m_Text, keyboardType, autocorrection: false, multiLine, secure: true, isAlert, "", characterLimit) : TouchScreenKeyboard.Open(m_Text, keyboardType, inputType == InputType.AutoCorrect, multiLine, secure: false, isAlert, "", characterLimit));
				OnFocus();
				if (m_SoftKeyboard != null && m_SoftKeyboard.canSetSelection)
				{
					int length = ((stringPositionInternal < stringSelectPositionInternal) ? (stringSelectPositionInternal - stringPositionInternal) : (stringPositionInternal - stringSelectPositionInternal));
					m_SoftKeyboard.selection = new RangeInt((stringPositionInternal < stringSelectPositionInternal) ? stringPositionInternal : stringSelectPositionInternal, length);
				}
			}
		}
		else
		{
			if (!TouchScreenKeyboardShouldBeUsed() && !m_ReadOnly && inputSystem != null)
			{
				inputSystem.imeCompositionMode = (IMECompositionMode)1;
			}
			OnFocus();
		}
		m_AllowInput = true;
		m_OriginalText = text;
		m_WasCanceled = false;
		SetCaretVisible();
		UpdateLabel();
	}

	public override void OnSelect(BaseEventData eventData)
	{
		base.OnSelect(eventData);
		SendOnFocus();
		if (shouldActivateOnSelect)
		{
			ActivateInputField();
		}
	}

	public virtual void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			ActivateInputField();
		}
	}

	public void OnControlClick()
	{
	}

	public void ReleaseSelection()
	{
		m_SelectionStillActive = false;
		m_ReleaseSelection = false;
		m_PreviouslySelectedObject = null;
		MarkGeometryAsDirty();
		SendOnEndEdit();
		SendOnEndTextSelection();
	}

	public void DeactivateInputField(bool clearSelection = false)
	{
		if ((codeCompleter != null && codeCompleter.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(codeCompleter, (Vector2)Input.mousePosition, uiCam)) || !m_AllowInput)
		{
			return;
		}
		m_HasDoneFocusTransition = false;
		m_AllowInput = false;
		if (m_Placeholder != null)
		{
			m_Placeholder.enabled = string.IsNullOrEmpty(m_Text);
		}
		if ((UnityEngine.Object)(object)m_TextComponent != null && IsInteractable())
		{
			if (m_WasCanceled && m_RestoreOriginalTextOnEscape && !m_IsKeyboardBeingClosedInHoloLens)
			{
				text = m_OriginalText;
			}
			if (m_SoftKeyboard != null)
			{
				m_SoftKeyboard.active = false;
				m_SoftKeyboard = null;
			}
			m_SelectionStillActive = true;
			if ((m_ResetOnDeActivation || m_ReleaseSelection || clearSelection) && m_VerticalScrollbar == null)
			{
				ReleaseSelection();
			}
			if (inputSystem != null)
			{
				inputSystem.imeCompositionMode = (IMECompositionMode)0;
			}
			m_IsKeyboardBeingClosedInHoloLens = false;
		}
		MarkGeometryAsDirty();
	}

	public override void OnDeselect(BaseEventData eventData)
	{
		DeactivateInputField();
		base.OnDeselect(eventData);
		SendOnFocusLost();
	}

	public virtual void OnSubmit(BaseEventData eventData)
	{
		if (IsActive() && IsInteractable())
		{
			if (!isFocused)
			{
				m_ShouldActivateNextUpdate = true;
			}
			SendOnSubmit();
			DeactivateInputField();
			eventData?.Use();
		}
	}

	public virtual void OnCancel(BaseEventData eventData)
	{
		if (IsActive() && IsInteractable())
		{
			if (!isFocused)
			{
				m_ShouldActivateNextUpdate = true;
			}
			m_WasCanceled = true;
			DeactivateInputField();
			eventData.Use();
		}
	}

	public override void OnMove(AxisEventData eventData)
	{
		if (!m_AllowInput)
		{
			base.OnMove(eventData);
		}
	}

	private void EnforceContentType()
	{
		switch (contentType)
		{
		case ContentType.Standard:
			m_InputType = InputType.Standard;
			m_KeyboardType = TouchScreenKeyboardType.Default;
			m_CharacterValidation = CharacterValidation.None;
			break;
		case ContentType.Autocorrected:
			m_InputType = InputType.AutoCorrect;
			m_KeyboardType = TouchScreenKeyboardType.Default;
			m_CharacterValidation = CharacterValidation.None;
			break;
		case ContentType.IntegerNumber:
			m_LineType = LineType.SingleLine;
			m_InputType = InputType.Standard;
			m_KeyboardType = TouchScreenKeyboardType.NumberPad;
			m_CharacterValidation = CharacterValidation.Integer;
			break;
		case ContentType.DecimalNumber:
			m_LineType = LineType.SingleLine;
			m_InputType = InputType.Standard;
			m_KeyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
			m_CharacterValidation = CharacterValidation.Decimal;
			break;
		case ContentType.Alphanumeric:
			m_LineType = LineType.SingleLine;
			m_InputType = InputType.Standard;
			m_KeyboardType = TouchScreenKeyboardType.ASCIICapable;
			m_CharacterValidation = CharacterValidation.Alphanumeric;
			break;
		case ContentType.Name:
			m_LineType = LineType.SingleLine;
			m_InputType = InputType.Standard;
			m_KeyboardType = TouchScreenKeyboardType.Default;
			m_CharacterValidation = CharacterValidation.Name;
			break;
		case ContentType.EmailAddress:
			m_LineType = LineType.SingleLine;
			m_InputType = InputType.Standard;
			m_KeyboardType = TouchScreenKeyboardType.EmailAddress;
			m_CharacterValidation = CharacterValidation.EmailAddress;
			break;
		case ContentType.Password:
			m_LineType = LineType.SingleLine;
			m_InputType = InputType.Password;
			m_KeyboardType = TouchScreenKeyboardType.Default;
			m_CharacterValidation = CharacterValidation.None;
			break;
		case ContentType.Pin:
			m_LineType = LineType.SingleLine;
			m_InputType = InputType.Password;
			m_KeyboardType = TouchScreenKeyboardType.NumberPad;
			m_CharacterValidation = CharacterValidation.Digit;
			break;
		}
		SetTextComponentWrapMode();
	}

	private void SetTextComponentWrapMode()
	{
		_ = (UnityEngine.Object)(object)m_TextComponent == null;
	}

	private void SetTextComponentRichTextMode()
	{
		if (!((UnityEngine.Object)(object)m_TextComponent == null))
		{
			m_TextComponent.richText = m_RichText;
		}
	}

	private void SetToCustomIfContentTypeIsNot(params ContentType[] allowedContentTypes)
	{
		if (contentType == ContentType.Custom)
		{
			return;
		}
		for (int i = 0; i < allowedContentTypes.Length; i++)
		{
			if (contentType == allowedContentTypes[i])
			{
				return;
			}
		}
		contentType = ContentType.Custom;
	}

	private void SetToCustom()
	{
		if (contentType != ContentType.Custom)
		{
			contentType = ContentType.Custom;
		}
	}

	private void SetToCustom(CharacterValidation characterValidation)
	{
		if (contentType == ContentType.Custom)
		{
			characterValidation = CharacterValidation.CustomValidator;
			return;
		}
		contentType = ContentType.Custom;
		characterValidation = CharacterValidation.CustomValidator;
	}

	protected override void DoStateTransition(SelectionState state, bool instant)
	{
		if (m_HasDoneFocusTransition)
		{
			state = SelectionState.Selected;
		}
		else if (state == SelectionState.Pressed)
		{
			m_HasDoneFocusTransition = true;
		}
		base.DoStateTransition(state, instant);
	}

	public virtual void CalculateLayoutInputHorizontal()
	{
	}

	public virtual void CalculateLayoutInputVertical()
	{
	}

	public void SetGlobalPointSize(float pointSize)
	{
		Graphic graphic = m_Placeholder;
		TMP_Text val = (TMP_Text)(object)((graphic is TMP_Text) ? graphic : null);
		if ((UnityEngine.Object)(object)val != null)
		{
			val.fontSize = pointSize;
		}
		textComponent.fontSize = pointSize;
	}

	public void SetGlobalFontAsset(TMP_FontAsset fontAsset)
	{
		Graphic graphic = m_Placeholder;
		TMP_Text val = (TMP_Text)(object)((graphic is TMP_Text) ? graphic : null);
		if ((UnityEngine.Object)(object)val != null)
		{
			val.font = fontAsset;
		}
		textComponent.font = fontAsset;
	}

	Transform ICanvasElement.get_transform()
	{
		return base.transform;
	}
}
