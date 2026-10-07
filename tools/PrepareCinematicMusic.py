from pathlib import Path
import wave,numpy as np,shutil,subprocess,os
from ExportCinematicVideo import ENCODER, ROOT
os.chdir(ROOT)
Path("work").mkdir(exist_ok=True)
subprocess.run([str(ENCODER), "-y", "-i", "docs/Dialoge/IntroCinematic/Charcoal Atmosphere.wav", "-ar", "22050", "-ac", "2", "work/charcoal-22050.wav"], check=True)
assets=Path('Assets/SpaceMiner/Resources/MemoryCinematic')
voice=Path('docs/Dialoge/IntroCinematic/00_Gesamter_Sprechertext_Enceladus.wav');shutil.copyfile(voice,assets/'narration.wav')
with wave.open(str(voice)) as w:r=w.getframerate();v=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(float)/32768;n=len(v)
with wave.open('work/charcoal-22050.wav') as w:m=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').reshape(-1,2).astype(float)/32768
music=np.zeros((n+int(.5*r),2));music[:min(len(m),len(music))]=m[:len(music)]
block=220;control=[];duck=0
for i in range(0,len(music),block):
 speech=v[i:min(i+block,n)];target=1 if len(speech) and np.sqrt(np.mean(speech**2))>.006 else 0
 duck+=(target-duck)*(1-np.exp(-.01/(.08 if target>duck else .5)));control.extend([.075*(1-.45*duck)]*min(block,len(music)-i))
t=np.arange(len(music))/r;fade=np.minimum(np.clip(t/3,0,1),np.clip((min(len(m),len(music))/r-t)/5,0,1));music*=np.array(control)[:,None]*fade[:,None]
with wave.open(str(assets/'atmosphere.wav'),'wb') as w:w.setparams((2,2,r,0,'NONE','not compressed'));w.writeframes(np.round(np.clip(music,-1,1)*32767).astype('<i2').tobytes())
print('Music prepared: 7.5% base gain, 4.125% under speech, 3s in / 5s out, no voice fades')
