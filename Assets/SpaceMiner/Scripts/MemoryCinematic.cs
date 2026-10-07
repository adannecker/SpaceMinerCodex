using System;
using UnityEngine;

namespace SpaceMiner
{
    // Remembered pages: the mesh bends the paper while UV motion explores each drawing.
    public sealed class MemoryCinematic : MonoBehaviour
    {
        public static bool IsPlaying { get; private set; }
        public event Action Finished;
        public const int SceneCount = 12;
        public const float SceneGap = 0;
        public const float DissolveSeconds = 1.2f;
        readonly string[] names = { "Unsere alte Welt", "Der Blick zu den Sternen", "Grenzen \u00fcberwinden", "Eine neue Energiequelle", "Eine Zukunft ohne Grenzen", "Die Warnungen", "Wir irrten uns", "Die Evakuierung", "Unsere Welt zerbrach", "Die Stille", "Am Rande des Asteroideng\u00fcrtels", "Nun liegt es an dir" };
        readonly string[] captions = { "Einst waren wir eine hoch entwickelte Kultur.\nTechnologie hatte unsere Welt ver\u00e4ndert.\nDoch trotz all unseres Fortschritts hatten wir nie vergessen, woher wir kamen.\nWir lebten mit unseren Familien, unserer Natur.", "Aber wir waren neugierig.\nSeit Generationen blickten wir zu den Sternen.\nWir wollten verstehen, was dort drau\u00dfen auf uns wartete.", "Wir wollten unsere Grenzen \u00fcberwinden. Weiter reisen. Weiter forschen.\nDas Unm\u00f6gliche m\u00f6glich machen.", "Dann glaubten wir, etwas gefunden zu haben, das alles ver\u00e4ndern w\u00fcrde.\nEine neue Energiequelle.", "M\u00e4chtig genug, um unsere Welt nahezu unbegrenzt mit Energie zu versorgen \u2026 und uns den Weg zu den Sternen zu \u00f6ffnen.\nWir sahen eine Zukunft ohne Grenzen.", "Doch es gab Warnungen. Messwerte, die wir nicht verstanden. Wissenschaftler, die uns zur Vorsicht mahnten. Stimmen, die verlangten, das Projekt zu stoppen.", "Wir h\u00f6rten sie. Aber unser Drang nach Fortschritt war st\u00e4rker. Wir irrten uns.", "Eine Kettenreaktion hatte begonnen. Als wir begriffen, was geschehen w\u00fcrde, versuchten wir zu retten, was noch zu retten war. Wir evakuierten unsere Welt. Menschen. Tiere. Samen. DNA. Wissen. Alles, was von uns bleiben konnte.", "Doch es war zu sp\u00e4t. Noch w\u00e4hrend die letzten Schiffe starteten \u2026 zerbrach unsere Welt.", "Seitdem herrscht Stille. Kein Signal. Keine Antwort. Niemand scheint \u00fcberlebt zu haben. Niemand \u2026 au\u00dfer dir.", "Wochen zuvor warst du aufgebrochen. Allein. In einer kleinen Raumstation am Rande eines Asteroideng\u00fcrtels. Weit genug entfernt, um zu \u00fcberleben. Nicht weit genug, um unversehrt zu bleiben.", "Jetzt bist du allein. Deine Station ist besch\u00e4digt. Deine Vorr\u00e4te sind begrenzt. Und dort drau\u00dfen liegen die \u00dcberreste unserer Welt. Nun liegt es an dir \u2026 zu retten, was noch zu retten ist." };
        readonly Vector2[] focus = { new Vector2(.42f,.53f), new Vector2(.49f,.68f), new Vector2(.43f,.51f), new Vector2(.69f,.56f), new Vector2(.58f,.64f), new Vector2(.54f,.43f), new Vector2(.67f,.53f), new Vector2(.62f,.44f), new Vector2(.53f,.55f), new Vector2(.47f,.40f), new Vector2(.43f,.51f), new Vector2(.54f,.44f) };
        [Serializable] sealed class Timing { public float[] starts; }
        Texture2D[] pictures; AudioClip narration; AudioSource voice, atmosphere;
        float[] starts; float totalTime; bool audioPaused;
        GameObject stage; Camera view; Mesh mesh; Material paper; Texture2D mask;
        int index; float elapsed;
        public int SceneIndex => index;
        public bool Play()
        {
            if (IsPlaying) return false;
            pictures = new Texture2D[SceneCount];
            narration=Resources.Load<AudioClip>("MemoryCinematic/narration");
            var timing=Resources.Load<TextAsset>("MemoryCinematic/timing");
            if(narration==null || timing==null)return false;
            starts=JsonUtility.FromJson<Timing>(timing.text).starts;
            if(starts==null || starts.Length!=SceneCount)return false;
            for (int i=0;i<SceneCount;i++)
            {
                pictures[i]=Resources.Load<Texture2D>("MemoryCinematic/scene"+(i+1));
                if (pictures[i]==null) { Debug.LogError("Missing cinematic scene "+(i+1)); return false; }
            }
            if (voice==null) { voice=gameObject.AddComponent<AudioSource>(); voice.playOnAwake=false; voice.spatialBlend=0; voice.volume=.85f; PlayerAudioSource.Attach(voice,PlayerAudioChannel.Voices); }
            if(atmosphere==null){atmosphere=gameObject.AddComponent<AudioSource>();atmosphere.playOnAwake=false;atmosphere.spatialBlend=0;atmosphere.volume=1;PlayerAudioSource.Attach(atmosphere,PlayerAudioChannel.Background);}
            atmosphere.clip=Resources.Load<AudioClip>("MemoryCinematic/atmosphere");
            if(atmosphere.clip==null)return false;
            stage=new GameObject("Remembered charcoal pages");
            view=new GameObject("Memory camera",typeof(Camera)).GetComponent<Camera>(); view.transform.SetParent(stage.transform);
            view.transform.position=new Vector3(0,0,-10); view.orthographic=true; view.orthographicSize=3.80f; view.clearFlags=CameraClearFlags.SolidColor; view.backgroundColor=new Color(.035f,.031f,.025f); view.depth=100; view.cullingMask=1<<30;
            mesh=new Mesh(); const int nx=64,ny=36; var vertices=new Vector3[(nx+1)*(ny+1)]; var uv=new Vector2[vertices.Length]; var tris=new int[nx*ny*6]; int t=0;
            for(int y=0;y<=ny;y++) for(int x=0;x<=nx;x++) { int k=y*(nx+1)+x; float u=x/(float)nx,v=y/(float)ny; vertices[k]=new Vector3((u-.5f)*10.7f,(v-.5f)*6.02f,.17f*Mathf.Sin(u*5+v*3)+.28f*Mathf.Pow(Mathf.Abs(u-.5f)*2,6)); uv[k]=new Vector2(u,v); if(x<nx&&y<ny){ tris[t++]=k;tris[t++]=k+nx+1;tris[t++]=k+1;tris[t++]=k+1;tris[t++]=k+nx+1;tris[t++]=k+nx+2; } }
            mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=tris;mesh.RecalculateNormals();mesh.RecalculateBounds();
            mask=new Texture2D(512,288,TextureFormat.RGBA32,false);
            for(int y=0;y<288;y++)for(int x=0;x<512;x++){float u=x/511f,v=y/287f;float edge=Mathf.Min(Mathf.Min(u,1-u),Mathf.Min(v,1-v));float torn=.009f+.015f*Mathf.PerlinNoise(u*57,v*43)+.004f*Mathf.Sin(u*230+v*170);mask.SetPixel(x,y,new Color(1,1,1,Mathf.SmoothStep(0,1,(edge-torn)/.007f)));}mask.Apply();
            paper=new Material(Shader.Find("SpaceMiner/RememberedPaper"));paper.SetTexture("_EdgeMask",mask);
            var page=new GameObject("Uneven torn paper",typeof(MeshFilter),typeof(MeshRenderer));page.layer=30;page.transform.SetParent(stage.transform);page.GetComponent<MeshFilter>().sharedMesh=mesh;page.GetComponent<MeshRenderer>().sharedMaterial=paper;
            IsPlaying=true;index=0;totalTime=0;elapsed=0;audioPaused=false;paper.mainTexture=pictures[0];voice.clip=narration;voice.Play();atmosphere.Play();ApplyVisual(0);return true;
        }
        void Update()
        {
            if(!IsPlaying)return;
            if(Input.GetKeyDown(KeyCode.Escape)){Stop();return;}
            if(SettingsMenu.IsOpen){if(!audioPaused){voice.Pause();atmosphere.Pause();audioPaused=true;}return;}
            if(audioPaused){voice.UnPause();atmosphere.UnPause();audioPaused=false;}
            totalTime=voice.isPlaying?voice.time:totalTime+Time.unscaledDeltaTime;
            index=0;while(index<SceneCount-1 && totalTime>=starts[index+1])index++;
            elapsed=totalTime-starts[index];paper.mainTexture=pictures[index];ApplyVisual(elapsed);
            if(totalTime>=narration.length+.5f && !voice.isPlaying)Stop();
        }
        void ApplyVisual(float seconds)
        {
            float duration=SceneDuration(index),p=Mathf.Clamp01(seconds/duration);
            float zoom=Mathf.Lerp(1.015f,1.14f,Mathf.SmoothStep(0,1,p));paper.mainTextureScale=Vector2.one/zoom;paper.mainTextureOffset=(Vector2.one-paper.mainTextureScale)*Vector2.Lerp(new Vector2(.5f,.5f),focus[index],p);
            float blend=index==0?1:Mathf.SmoothStep(0,1,seconds/DissolveSeconds);
            paper.SetFloat("_Blend",blend);
            paper.SetTexture("_PreviousTex",pictures[Mathf.Max(0,index-1)]);
            float oldScale=1/1.14f;
            Vector2 oldOffset=(Vector2.one-Vector2.one*oldScale)*focus[Mathf.Max(0,index-1)];
            paper.SetVector("_PreviousUV",new Vector4(oldScale,oldScale,oldOffset.x,oldOffset.y));
            // Only the beginning/end of the entire film fades; scene changes keep the page opaque.
            float fade=index==0?Mathf.SmoothStep(0,1,seconds/.35f):1;
            if(index==SceneCount-1)fade*=Mathf.SmoothStep(0,1,(duration-seconds)/.35f);
            paper.SetFloat("_Fade",fade);
            var page=stage.transform.GetChild(1);
            Quaternion rotation=Quaternion.Euler(2*Mathf.Sin(p*2),-3+5*p,(index==1?3.2f:-3.0f)+.65f*p);
            Quaternion previousRotation=Quaternion.Euler(2*Mathf.Sin(2),2,((index-1)==1?3.2f:-3.0f)+.65f);
            page.localRotation=index==0?rotation:Quaternion.Slerp(previousRotation,rotation,blend);
            view.orthographicSize=3.80f*Mathf.Max(1,1.64f/view.aspect);
        }
        public float SceneStart(int scene) => starts[scene];
        public float SceneDuration(int scene) => (scene<SceneCount-1?starts[scene+1]:narration.length+.5f)-starts[scene];
        public float VoiceDuration(int scene) => SceneDuration(scene);
        public bool UsesContinuousNarration => voice.clip==narration;
        public Camera ExportCamera => view;
        public float CrossDissolve => paper.GetFloat("_Blend");
        public float PageOpacity => paper.GetFloat("_Fade");
        public void SampleExport(int scene, float seconds)
        {
            index=scene;elapsed=seconds;paper.mainTexture=pictures[index];voice.Stop();atmosphere.Stop();ApplyVisual(seconds);
        }
        public void Stop(){if(!IsPlaying)return;IsPlaying=false;voice.Stop();atmosphere.Stop();Destroy(stage);Destroy(mesh);Destroy(paper);Destroy(mask);Finished?.Invoke();}
        void OnDestroy(){Stop();}
        public string CurrentSubtitle
        {
            get {
                if(!IsPlaying || !SettingsStore.Current.Accessibility.Subtitles)return string.Empty;
                // Entire scene text avoids pretending that generated speech has exact word timestamps.
                return captions[index];
            }
        }
        void OnGUI()
        {
            if(!IsPlaying)return;GUI.depth=-300;
            GUI.color=new Color(.82f,.79f,.7f);GUI.Label(new Rect(30,20,Screen.width-60,25),"ERINNERUNGEN / "+(index+1)+" - "+names[index]+"                         ESC - Zur\u00fcck");GUI.color=Color.white;
            if(string.IsNullOrEmpty(CurrentSubtitle))return;
            var style=new GUIStyle(GUI.skin.label){fontSize=Mathf.RoundToInt(22*SettingsStore.Current.Accessibility.TextScale),alignment=TextAnchor.MiddleCenter,wordWrap=true};style.normal.textColor=new Color(.96f,.93f,.85f);
            var rect=new Rect(Screen.width*.12f,Screen.height-125,Screen.width*.76f,94);
            GUI.color=new Color(0,0,0,.82f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.Label(new Rect(rect.x+16,rect.y+8,rect.width-32,rect.height-16),CurrentSubtitle,style);
        }
    }
}
