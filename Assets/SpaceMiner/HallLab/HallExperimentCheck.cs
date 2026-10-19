using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SpaceMiner
{
    public sealed class HallExperimentCheck : MonoBehaviour
    {
        private int checks;
        private string Folder=>Path.Combine(Environment.CurrentDirectory,"Logs/HallExperiment");
        private HallExperimentView app;
        private IEnumerator Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-hallExperimentCheck")<0)yield break;
            Directory.CreateDirectory(Folder);string result=Path.Combine(Folder,"result.json");if(File.Exists(result))File.Delete(result);
            var run=Run();
            while(true)
            {
                bool next;object current;
                try{next=run.MoveNext();current=next?run.Current:null;}
                catch(Exception e){Debug.LogException(e);Debug.LogError("HALL EXPERIMENT CHECK FAILED");Application.Quit(1);yield break;}
                if(!next)yield break;yield return current;
            }
        }
        private IEnumerator Run()
        {
            yield return null;app=GetComponent<HallExperimentView>();yield return new WaitForSecondsRealtime(1.2f);
            Require(app!=null && app.HallRoot!=null && app.Walker!=null && app.ViewCamera!=null,"complete metre-scale scene");
            Require(FindFirstObjectByType<WaterScenario>()==null && FindFirstObjectByType<RuinedWorld>()==null,"isolated from campaign and saved gameplay");
            Require(FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1,"one audio listener");
            Require(app.DoorFrames.Length==3 && app.DoorAmounts[1]>.99f && app.DoorAmounts[0]<.01f,"three portals, central door initially open");
            Require(Mathf.Abs(app.DoorFrames[1].position.y-1.7f)<.05f && app.DoorFrames[1].position.z>15 && app.DoorFrames[1].position.z<17,"Blender export preserves orientation and metre scale");
            var filters=app.HallRoot.GetComponentsInChildren<MeshFilter>(true);
            Require(filters.Length>=100 && filters.Sum(f=>f.sharedMesh.vertexCount)>40000,"actual detailed Blender geometry imported");
            Require(app.ScaleDrone.GetComponentsInChildren<Renderer>().Length>0,"existing drone provides a scale reference");
            Require(app.HallSurfaces.Length==app.DetailedMaterials.Length && app.HallSurfaces.All(r=>r.sharedMaterial!=null),"complete material assignments");
            var paint=app.DetailedMaterials.First(m=>m.name.StartsWith("Paint_Ivory"));
            Require(paint.mainTexture!=null && paint.mainTexture.width>=1024 && paint.GetTexture("_BumpMap")!=null && paint.GetTexture("_MetallicGlossMap")!=null,"real albedo, normal and metallic/smoothness maps");
            Require(app.DetailedMaterials.Any(m=>m.IsKeywordEnabled("_EMISSION")),"light and status surfaces emit");
            foreach(float x in new[]{-5f,0,5f})foreach(float z in new[]{1f,7f,12f})
            {
                Require(Physics.Raycast(new Vector3(x,3,z),Vector3.down,out var hit,4) && hit.point.y>=-.05f,"continuous walkable floor at "+x+","+z);
            }
            // Capture actual rendered scenes after the camera and shader state settle.
            app.SetView(0);yield return new WaitForSecondsRealtime(.4f);
            ScreenCapture.CaptureScreenshot(Path.Combine(Folder,"01-overview.png"));yield return new WaitForEndOfFrame();yield return null;
            app.SetView(3);yield return new WaitForSecondsRealtime(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(Folder,"02-material-detail.png"));yield return new WaitForEndOfFrame();yield return null;
            app.SetView(1);yield return new WaitForSecondsRealtime(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(Folder,"03-drone-scale.png"));yield return new WaitForEndOfFrame();yield return null;
            app.SetView(0);app.SetDetailed(false);
            Require(app.HallSurfaces.All(r=>r.sharedMaterial==app.PlainMaterial),"material comparison actually replaces all surfaces");
            yield return new WaitForSecondsRealtime(.25f);ScreenCapture.CaptureScreenshot(Path.Combine(Folder,"04-simple-surfaces.png"));yield return new WaitForEndOfFrame();yield return null;
            app.SetDetailed(true);
            Require(app.HallSurfaces.Select((r,i)=>r.sharedMaterial==app.DetailedMaterials[i]).All(v=>v),"original materials restore exactly");
            app.SetDetails(false);Require(app.HallSurfaces.Where(r=>r.name.StartsWith("Fasteners__")).All(r=>!r.enabled),"detail comparison hides fasteners");
            app.SetDetails(true);Require(app.HallSurfaces.All(r=>r.enabled),"all detail renderers restored");
            app.SetLighting(1);Require(app.WorkLights.All(l=>l.intensity>2.5f && l.color.b>l.color.r),"inspection light state");
            yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(Path.Combine(Folder,"05-inspection-light.png"));yield return new WaitForEndOfFrame();yield return null;
            app.SetLighting(2);Require(app.WorkLights.All(l=>l.intensity<.4f && l.color.r>l.color.b),"emergency light state");
            yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(Path.Combine(Folder,"06-emergency-light.png"));yield return new WaitForEndOfFrame();yield return null;
            app.SetLighting(0);
            // Walk toward an actually closed leaf; the capsule must stop, then pass after opening.
            Vector3 center=app.DoorFrames[0].position;center.y=.02f;
            Vector3 outward=new Vector3(center.x,0,center.z+38).normalized;
            app.LookFrom(center-outward*2+Vector3.up*1.65f,center+Vector3.up*1.65f);
            for(int i=0;i<120;i++){app.Move(Vector2.up,.02f,false);yield return null;}
            Require(Vector3.Dot(app.Feet-center,outward)<-.20f,"closed door blocks the player capsule");
            Vector3 closed=app.DoorLeft[0].position;
            Require(app.RequestDoor(0,true),"opening accepted");
            float deadline=Time.realtimeSinceStartup+6;
            while(app.DoorAmounts[0]<.999f && Time.realtimeSinceStartup<deadline)yield return null;
            Require(app.DoorAmounts[0]>.999f && Vector3.Distance(closed,app.DoorLeft[0].position)>1.6f,"door slides into its pocket");
            Physics.SyncTransforms();
            app.LookFrom(center-outward*1.3f+Vector3.up*1.65f,center+Vector3.up*1.65f);
            for(int i=0;i<75;i++){app.Move(Vector2.up,.02f,false);yield return null;}
            Require(Vector3.Dot(app.Feet-center,outward)>.65f && app.Feet.y>-.10f,"player passes through open portal onto apron");
            app.LookFrom(center+Vector3.up*1.65f,center+outward+Vector3.up*1.65f);
            Require(!app.RequestDoor(0,false) && app.DoorTargets[0],"occupied doorway prevents closing");
            app.SetView(2);Require(app.RequestDoor(0,false),"clear doorway accepts closing");
            deadline=Time.realtimeSinceStartup+6;
            while(app.DoorAmounts[0]>.001f && Time.realtimeSinceStartup<deadline)yield return null;
            Require(app.DoorAmounts[0]<.001f && Vector3.Distance(closed,app.DoorLeft[0].position)<.002f,"door returns to exact closed position");
            ScreenCapture.CaptureScreenshot(Path.Combine(Folder,"07-airlocks.png"));yield return new WaitForEndOfFrame();yield return null;
            app.LookFrom(new Vector3(0,1.72f,3),new Vector3(0,1.72f,14));float before=app.Vitals.Stamina;Vector3 start=app.Feet;
            for(int i=0;i<15;i++){app.Move(Vector2.up,.02f,true);yield return null;}
            Require(app.Feet.z>start.z+1 && app.Vitals.Stamina<before,"development sprint moves and consumes stamina");
            app.SetCapture(true);Require(app.Captured,"walk mode enabled");app.SetCapture(false);Require(!app.Captured && Cursor.visible && Cursor.lockState==CursorLockMode.None,"inspection releases mouse");
            Require(app.GetComponent<DisplayModeController>()!=null,"shared fullscreen controller present");
            app.SetView(0);yield return new WaitForSecondsRealtime(.3f);
            File.WriteAllText(Path.Combine(Folder,"result.json"),JsonUtility.ToJson(new Result{checks=checks,meshObjects=filters.Length,vertices=filters.Sum(f=>f.sharedMesh.vertexCount),renderers=app.HallSurfaces.Length},true));
            Debug.Log("HALL EXPERIMENT CHECK PASSED: "+checks+" checks");Application.Quit(0);
        }
        private void Require(bool value,string message){if(!value)throw new Exception(message);checks++;}
        [Serializable] private sealed class Result{public bool passed=true,isolatedFromCampaign=true;public int checks,meshObjects,vertices,renderers;public int doors=3;}
    }
}
