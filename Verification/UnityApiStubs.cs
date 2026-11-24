// Minimal signature stubs of the Unity APIs used by the project (compile-check only).
using System;
using System.Collections;
using System.Collections.Generic;
namespace UnityEngine {
  public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} public static Vector2 zero=>default; public static Vector2 operator*(Vector2 a,float b)=>a; }
  public struct Vector4 { public Vector4(float x,float y,float z,float w){} }
  public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
    public static Vector3 zero=>default; public static Vector3 one=>default; public static Vector3 up=>default; public static Vector3 forward=>default; public static Vector3 back=>default; public static Vector3 right=>default;
    public float sqrMagnitude=>0; public Vector3 normalized=>this;
    public static Vector3 operator+(Vector3 a,Vector3 b)=>a; public static Vector3 operator-(Vector3 a,Vector3 b)=>a; public static Vector3 operator*(Vector3 a,float b)=>a; public static Vector3 operator*(float b,Vector3 a)=>a;
    public static bool operator==(Vector3 a,Vector3 b)=>true; public static bool operator!=(Vector3 a,Vector3 b)=>false; public override bool Equals(object o)=>true; public override int GetHashCode()=>0;
    public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a; }
  public struct Quaternion { public static Quaternion identity=>default; public Vector3 eulerAngles=>default;
    public static Quaternion Euler(float x,float y,float z)=>default; public static Quaternion LookRotation(Vector3 f,Vector3 u)=>default; public static Quaternion AngleAxis(float a,Vector3 axis)=>default;
    public static Quaternion Inverse(Quaternion q)=>q; public static Quaternion Slerp(Quaternion a,Quaternion b,float t)=>a;
    public static Quaternion operator*(Quaternion a,Quaternion b)=>a; public static Vector3 operator*(Quaternion a,Vector3 b)=>b; }
  public struct Color { public float r,g,b,a; public Color(float r,float g,float b){this.r=r;this.g=g;this.b=b;a=1;} public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;} public static Color white=>default; }
  public struct Color32 { public byte r,g,b,a; public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;} public static implicit operator Color32(Color c)=>default; public static implicit operator Color(Color32 c)=>default; }
  public struct Rect { public Rect(float x,float y,float w,float h){} }
  public static class ColorUtility { public static string ToHtmlStringRGB(Color c)=>""; }
  public static class Mathf { public const float PI=3.14f, Deg2Rad=0.017f; public static float Sin(float f)=>0; public static float Cos(float f)=>0; public static float Sqrt(float f)=>0; public static float Pow(float a,float b)=>0;
    public static float Clamp(float v,float a,float b)=>v; public static int Clamp(int v,int a,int b)=>v; public static float Clamp01(float v)=>v; public static float Lerp(float a,float b,float t)=>a; public static float Repeat(float a,float b)=>a;
    public static float Min(float a,float b)=>a<b?a:b; public static int Min(int a,int b)=>a<b?a:b; public static float Max(float a,float b)=>a>b?a:b; public static int Max(int a,int b)=>a>b?a:b; public static int FloorToInt(float f)=>0; public static int RoundToInt(float f)=>0; }
  public static class Random { public static int Range(int a,int b)=>a; public static float Range(float a,float b)=>a; public static Vector3 onUnitSphere=>default; }
  public static class Time { public static float deltaTime=>0; public static float unscaledDeltaTime=>0; }
  public static class Debug { public static void LogException(System.Exception e){} public static void Log(object o){} public static void LogWarning(object o){} }
  public class Object { public string name; public static void Destroy(Object o){} public static T FindAnyObjectByType<T>() where T:Object=>null;
    public static implicit operator bool(Object o)=>true; public static bool operator==(Object a,Object b)=>true; public static bool operator!=(Object a,Object b)=>false; public override bool Equals(object o)=>true; public override int GetHashCode()=>0; }
  public class Component : Object { public Transform transform=>null; public GameObject gameObject=>null; public T GetComponent<T>()=>default; public T GetComponentInChildren<T>(bool inactive)=>default; public string tag; }
  public class Behaviour : Component { public bool enabled; public bool isActiveAndEnabled=>true; }
  public class MonoBehaviour : Behaviour { public Coroutine StartCoroutine(IEnumerator e){ Run(e); return null; } static void Run(IEnumerator e){ while(e.MoveNext()){ var n=e.Current as IEnumerator; if(n!=null) Run(n);} } public void StopAllCoroutines(){} }
  public class Coroutine {} public class YieldInstruction {} public class WaitForSeconds:YieldInstruction{ public WaitForSeconds(float s){} } public class WaitForSecondsRealtime:IEnumerator{ public WaitForSecondsRealtime(float s){} public object Current=>null; public bool MoveNext()=>false; public void Reset(){} }
  public sealed class GameObject : Object { public GameObject(){} public GameObject(string n){} public GameObject(string n, params Type[] c){} public Transform transform=>null; public string tag;
    public T AddComponent<T>() where T:Component=>null; public T GetComponent<T>()=>default; public void SetActive(bool b){} public static GameObject CreatePrimitive(PrimitiveType t)=>null; }
  public enum PrimitiveType{Sphere,Capsule,Cylinder,Cube,Plane,Quad}
  public class Transform : Component, IEnumerable { public Vector3 position,localPosition,localScale; public Quaternion rotation,localRotation; public Transform parent{get;set;} public void SetParent(Transform p,bool worldStays){} public Vector3 TransformPoint(Vector3 p)=>p; public IEnumerator GetEnumerator()=>null; }
  public class RectTransform : Transform { public Vector2 anchorMin,anchorMax,pivot,anchoredPosition,sizeDelta; }
  public class Mesh : Object { public Vector3[] vertices,normals; public int[] triangles; public void RecalculateBounds(){} }
  public class Shader : Object { public bool isSupported=>true; public static Shader Find(string n)=>null; }
  public class Material : Object { public Material(Material m){} public Material(Shader s){} public Shader shader; public bool enableInstancing; public bool HasProperty(string n)=>true; public void SetColor(string n,Color c){} public void SetFloat(string n,float f){} }
  public class MeshFilter : Component { public Mesh sharedMesh; }
  public class Renderer : Component { public Material sharedMaterial; public Material material; public Rendering.ShadowCastingMode shadowCastingMode; public bool receiveShadows; public Rendering.LightProbeUsage lightProbeUsage; public Rendering.ReflectionProbeUsage reflectionProbeUsage; }
  public class MeshRenderer : Renderer {} public class LineRenderer : Renderer { public int numCapVertices; }
  public class Collider : Component { public bool isTrigger; } public class SphereCollider:Collider{ public float radius; public Vector3 center; } public class BoxCollider:Collider{ public Vector3 size; }
  public class Rigidbody : Component { public bool isKinematic,useGravity; }
  public static class Resources { public static T Load<T>(string p) where T:Object=>null; public static T GetBuiltinResource<T>(string p) where T:Object=>null; }
  public class AudioClip:Object{} public enum AudioRolloffMode{Logarithmic,Linear,Custom}
  public class AudioSource:Behaviour{ public bool playOnAwake; public float spatialBlend,minDistance,maxDistance,dopplerLevel; public AudioRolloffMode rolloffMode; public void PlayOneShot(AudioClip c,float v){} }
  public class AudioListener:Behaviour{ public static float volume; }
  public enum CameraClearFlags{Skybox=1,SolidColor=2} public class Camera:Behaviour{ public float nearClipPlane,farClipPlane; public CameraClearFlags clearFlags; public Color backgroundColor; }
  public enum LightType{Spot,Directional,Point} public enum LightShadows{None,Hard,Soft} public class Light:Behaviour{ public LightType type; public float intensity; public Color color; public LightShadows shadows; }
  public enum ShadowQuality{Disable,HardOnly,All} public static class QualitySettings{ public static int antiAliasing; public static ShadowQuality shadows; }
  public static class RenderSettings { public static Rendering.AmbientMode ambientMode; public static Color ambientLight; public static bool fog; }
  public class Font:Object{ public static Font CreateDynamicFontFromOSFont(string n,int s)=>null; }
  public enum TextureFormat{RGBA32} public enum TextureWrapMode{Repeat,Clamp} public enum FilterMode{Point,Bilinear}
  public class Texture:Object{ public TextureWrapMode wrapMode; public FilterMode filterMode; } public class Texture2D:Texture{ public Texture2D(int w,int h,TextureFormat f,bool mip){} public void SetPixels32(Color32[] c){} public void Apply(){} }
  public enum SpriteMeshType{FullRect,Tight} public class Sprite:Object{ public static Sprite Create(Texture2D t,Rect r,Vector2 pivot,float ppu,uint extrude,SpriteMeshType m,Vector4 border)=>null; }
  public enum RenderMode{ScreenSpaceOverlay,ScreenSpaceCamera,WorldSpace} public class Canvas:Behaviour{ public RenderMode renderMode; public Camera worldCamera; }
  public enum TextAnchor{UpperLeft,UpperCenter,UpperRight,MiddleLeft,MiddleCenter,MiddleRight,LowerLeft,LowerCenter,LowerRight} public enum FontStyle{Normal,Bold} public enum HorizontalWrapMode{Wrap,Overflow} public enum VerticalWrapMode{Truncate,Overflow}
  public class Gradient{ public void SetKeys(GradientColorKey[] c,GradientAlphaKey[] a){} } public struct GradientColorKey{ public GradientColorKey(Color c,float t){} } public struct GradientAlphaKey{ public GradientAlphaKey(float a,float t){} }
  public enum RuntimeInitializeLoadType{AfterSceneLoad,BeforeSceneLoad} public class RuntimeInitializeOnLoadMethodAttribute:Attribute{ public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){} }
}
namespace UnityEngine.Rendering { public enum ShadowCastingMode{Off,On} public enum LightProbeUsage{Off,BlendProbes} public enum ReflectionProbeUsage{Off,BlendProbes} public enum AmbientMode{Skybox=0,Trilight=1,Flat=3}
  public class RenderPipelineAsset:Object{} public static class GraphicsSettings{ public static RenderPipelineAsset currentRenderPipeline=>null; } }
namespace UnityEngine.SceneManagement { public struct Scene{ public string name=>""; } public static class SceneManager{ public static Scene GetActiveScene()=>default; } }
namespace UnityEngine.Events { public class UnityEvent{ public void AddListener(UnityAction a){} } public delegate void UnityAction(); public delegate void UnityAction<T>(T a); public class UnityEvent<T>{ public void AddListener(UnityAction<T> a){} } }
namespace UnityEngine.EventSystems { public class UIBehaviour:MonoBehaviour{} public class EventSystem:UIBehaviour{} public class BaseInputModule:UIBehaviour{} }
namespace UnityEngine.UI {
  using UnityEngine.EventSystems;
  public class Graphic:UIBehaviour{ public Color color; public bool raycastTarget; public RectTransform rectTransform=>null; }
  public class Image:Graphic{ public enum Type{Simple,Sliced,Tiled,Filled} public Sprite sprite; public Type type; }
  public class Text:Graphic{ public Font font; public string text; public int fontSize,resizeTextMinSize,resizeTextMaxSize; public FontStyle fontStyle; public TextAnchor alignment; public HorizontalWrapMode horizontalOverflow; public VerticalWrapMode verticalOverflow; public float lineSpacing; public bool supportRichText,resizeTextForBestFit; }
  public struct ColorBlock{ public Color normalColor,highlightedColor,pressedColor,selectedColor,disabledColor; public float colorMultiplier,fadeDuration; }
  public struct Navigation{ public enum Mode{None,Horizontal,Vertical,Automatic,Explicit} public Mode mode; }
  public class Selectable:UIBehaviour{ public Graphic targetGraphic; public ColorBlock colors; public Navigation navigation; public bool interactable; }
  public class Button:Selectable{ public class ButtonClickedEvent:UnityEngine.Events.UnityEvent{} public ButtonClickedEvent onClick=>null; }
  public class CanvasScaler:UIBehaviour{ public float dynamicPixelsPerUnit,referencePixelsPerUnit; } public class GraphicRaycaster:UIBehaviour{}
}
namespace UnityEngine.XR { public static class XRSettings{ public static bool isDeviceActive=>false; public static string loadedDeviceName=>""; } }
namespace UnityEngine.InputSystem {
  public enum InputActionType{Value,Button,PassThrough}
  public class InputAction{ public InputAction(string name=null,InputActionType type=default,string binding=null,string interactions=null,string processors=null,string expectedControlType=null){} public void Enable(){} public event Action<CallbackContext> performed; public struct CallbackContext{} void U(){performed?.Invoke(default);} }
  public struct InputActionProperty{ public InputActionProperty(InputAction a){} }
  public static class InputActionSetupExtensions{ public struct BindingSyntax{} public static BindingSyntax AddBinding(this InputAction a,string path,string interactions=null,string processors=null,string groups=null)=>default; }
  public class InputControl<T>{ public T ReadValue()=>default; } namespace Controls{ public class ButtonControl{ public bool isPressed=>false; public bool wasPressedThisFrame=>false; } public class KeyControl:ButtonControl{} public class DeltaControl:InputControl<Vector2>{} }
  public class Keyboard{ public static Keyboard current=>null; public Controls.KeyControl spaceKey,enterKey,sKey,rKey; }
  public class Mouse{ public static Mouse current=>null; public Controls.ButtonControl rightButton; public Controls.DeltaControl delta; }
}
namespace UnityEngine.InputSystem.XR { public class TrackedPoseDriver:MonoBehaviour{ public InputActionProperty positionInput,rotationInput; } }
namespace Unity.XR.CoreUtils { using UnityEngine; public class XROrigin:MonoBehaviour{ public enum TrackingOriginMode{NotSpecified,Device,Floor} public Camera Camera{get;set;} public GameObject Origin{get;set;} public GameObject CameraFloorOffsetObject{get;set;} public TrackingOriginMode RequestedTrackingOriginMode{get;set;}
  public bool MatchOriginUpCameraForward(Vector3 up,Vector3 fwd)=>true; public bool MoveCameraToWorldLocation(Vector3 p)=>true; } }
namespace UnityEngine.XR.Interaction.Toolkit {
  using UnityEngine.Events; using UnityEngine.XR.Interaction.Toolkit.Interactables;
  public class XRInteractionManager:MonoBehaviour{ public virtual void CancelInteractableSelection(IXRSelectInteractable i){} }
  public struct InteractionLayerMask{ public static implicit operator InteractionLayerMask(int v)=>default; }
  public class SelectEnterEventArgs{} public class SelectExitEventArgs{} public class ActivateEventArgs{}
  public sealed class SelectEnterEvent:UnityEvent<SelectEnterEventArgs>{} public sealed class SelectExitEvent:UnityEvent<SelectExitEventArgs>{} public sealed class ActivateEvent:UnityEvent<ActivateEventArgs>{}
}
namespace UnityEngine.XR.Interaction.Toolkit.Interactables {
  public interface IXRSelectInteractable{}
  public abstract class XRBaseInteractable:MonoBehaviour,IXRSelectInteractable{ public enum MovementType{VelocityTracking,Kinematic,Instantaneous} public XRInteractionManager interactionManager{get;set;} public SelectEnterEvent selectEntered{get;set;} public SelectExitEvent selectExited{get;set;} public ActivateEvent activated{get;set;} public bool isSelected{get;private set;} }
  public class XRGrabInteractable:XRBaseInteractable{ public MovementType movementType{get;set;} public bool throwOnDetach{get;set;} public bool useDynamicAttach{get;set;} }
  public class XRSimpleInteractable:XRBaseInteractable{}
}
namespace UnityEngine.XR.Interaction.Toolkit.Inputs.Readers { using UnityEngine.InputSystem; public class XRInputButtonReader{ public enum InputSourceMode{Unused,InputAction,InputActionReference,ObjectReference,ManualValue} public InputSourceMode inputSourceMode{get;set;} public InputAction inputActionPerformed{get;set;} public InputAction inputActionValue{get;set;} } }
namespace UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics { using UnityEngine.InputSystem; public class XRInputHapticImpulseProvider{ public enum InputSourceMode{Unused,InputAction,InputActionReference,ObjectReference} public InputSourceMode inputSourceMode{get;set;} public InputAction inputAction{get;set;} }
  public class HapticImpulsePlayer:MonoBehaviour{ public XRInputHapticImpulseProvider hapticOutput{get;set;} public bool SendHapticImpulse(float a,float d)=>true; } }
namespace UnityEngine.XR.Interaction.Toolkit.Interactors { using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
  public abstract class XRBaseInteractor:MonoBehaviour{ public InteractionLayerMask interactionLayers{get;set;} }
  public abstract class XRBaseInputInteractor:XRBaseInteractor{ public XRInputButtonReader selectInput{get;set;} public XRInputButtonReader activateInput{get;set;} }
  public class XRDirectInteractor:XRBaseInputInteractor{ public bool improveAccuracyWithSphereCollider{get;set;} }
  public class XRRayInteractor:XRBaseInputInteractor{ public bool enableUIInteraction{get;set;} public float maxRaycastDistance{get;set;} public XRInputButtonReader uiPressInput{get;set;} } }
namespace UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals { public class XRInteractorLineVisual:MonoBehaviour{ public float lineWidth{get;set;} public bool overrideInteractorLineLength{get;set;} public float lineLength{get;set;} public bool stopLineAtFirstRaycastHit{get;set;} public Gradient validColorGradient{get;set;} public Gradient invalidColorGradient{get;set;} } }
namespace UnityEngine.XR.Interaction.Toolkit.UI { using UnityEngine.EventSystems; public class XRUIInputModule:BaseInputModule{} public class TrackedDeviceGraphicRaycaster:UIBehaviour{} }
namespace UnityEngine { public static class Application{ public static bool isMobilePlatform=>false; public static bool isEditor=>true; public static string dataPath=>""; } public class ScriptableObject:Object{ public static T CreateInstance<T>() where T:ScriptableObject=>null; } }
namespace UnityEngine.XR { public enum XRNode{LeftHand=4,RightHand=5} public struct InputDevice{ public bool isValid=>true; } public static class InputDevices{ public static InputDevice GetDeviceAtXRNode(XRNode n)=>default; } }
