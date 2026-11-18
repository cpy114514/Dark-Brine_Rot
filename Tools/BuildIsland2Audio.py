"""Offline, reproducible CC0 foley editing. No synthesis or file IO during gameplay."""
from pathlib import Path
import numpy as np
import soundfile as sf
import json, hashlib, shutil

RATE=32000
SRC=Path('.codex/island2-audio/sources')
OUT=Path('Assets/Resources/SecondIslandAudio')
ORIG=Path('Assets/ThirdParty/Island2Audio')
OUT.mkdir(parents=True,exist_ok=True);ORIG.mkdir(parents=True,exist_ok=True)
used={};outputs={};rng=np.random.default_rng(2172026)

def load(pack,name,speed=1):
    p=SRC/pack/name
    x,sr=sf.read(p,dtype='float32',always_2d=True);x=x.mean(axis=1)
    n=max(2,int(len(x)*RATE/sr/speed));x=np.interp(np.arange(n)*sr*speed/RATE,np.arange(len(x)),x)
    used[p.relative_to(SRC).as_posix()]=p
    return x-x.mean()

def eq(x,low=60,high=4200):
    f=np.fft.rfftfreq(len(x),1/RATE)
    hp=1-np.exp(-(f/max(1,low))**2);lp=1/np.sqrt(1+(f/high)**6)
    return np.fft.irfft(np.fft.rfft(x)*hp*lp,n=len(x))

def norm(x,rms=.12,peak=.78):
    x=np.nan_to_num(x);x-=x.mean(axis=0)
    x*=min(rms/max(1e-6,float(np.sqrt(np.mean(x*x)))),peak/max(1e-6,float(np.max(np.abs(x)))))
    return x

def fade(x,attack=.006,release=.055):
    x=x.copy();a=min(len(x)//3,int(attack*RATE));r=min(len(x)//2,int(release*RATE))
    x[:a]*=np.linspace(0,1,a);x[-r:]*=np.linspace(1,0,r);return x

def mix(parts,length=None):
    n=int(length*RATE) if length else max(len(x)+int(t*RATE) for x,t,g in parts)
    y=np.zeros(n)
    for x,t,g in parts:
        at=int(t*RATE);count=min(n-at,len(x))
        if count>0:y[at:at+count]+=x[:count]*g
    return y

def shot(name,parts,length=None,rms=.115):
    x=fade(norm(eq(mix(parts,length)),rms));sf.write(OUT/(name+'.wav'),x,RATE,subtype='PCM_16')
    outputs[name]={'seconds':round(len(x)/RATE,3),'peak':round(float(abs(x).max()),4),'rms':round(float(np.sqrt(np.mean(x*x))),4)}
    return x

def cyclic(x,length):
    cross=min(int(.6*RATE),len(x)//4);a=np.linspace(0,np.pi/2,cross)
    tile=np.concatenate([x[cross:-cross],x[-cross:]*np.cos(a)+x[:cross]*np.sin(a)])
    return np.resize(tile,int(length*RATE)).copy()

def loop(name,x,rms=.075,stereo=False):
    # A final overlap makes the exported PCM seam continuous.
    f=int(.8*RATE);a=np.linspace(0,np.pi/2,f)
    x=np.concatenate([x[f:-f],x[-f:]*np.cos(a)+x[:f]*np.sin(a)])
    x=norm(x,rms,.65)
    if stereo:x=np.stack([x,np.roll(x,int(.137*RATE))],axis=1)
    # Keep authoring masters as PCM; Unity imports long beds as streaming Vorbis.
    sf.write(OUT/(name+'.wav'),x,RATE,subtype='PCM_16')
    outputs[name]={'seconds':round(len(x)/RATE,3),'peak':round(float(abs(x).max()),4),'rms':round(float(np.sqrt(np.mean(x*x))),4),'loop':True}
    return x

impact=lambda n,s=1:load('impact-sounds','Audio/'+n+'.ogg',s)
ui=lambda n,s=1:load('interface-sounds','Audio/'+n+'.ogg',s)
rpg=lambda n,s=1:load('rpg-audio','Audio/'+n+'.ogg',s)
liquid=lambda n,s=1:load('liquid',n+'.ogg',s)
crunch=lambda i,s=1:load('crunch','impcrunch/impactcrunch%02d.mp3.flac'%i,s)

wind=load('wind','wind.ogg',.82)
windbed=loop('CloudWind',eq(cyclic(wind,36),110,2400),.065,True)
river=eq(cyclic(liquid('loop_water_02',.67),32),65,1150)*1.5+eq(cyclic(liquid('loop_bubbles_02',.72),32),80,1700)*.8
riverbed=loop('ChocolateFlow',river,.10)
bubblebed=loop('ChocolateBubbles',eq(cyclic(liquid('loop_bubbles_1',.62),24),100,1900),.085)

for i in range(4):
    soft=impact('footstep_carpet_%03d'%i,.91);snow=eq(impact('footstep_snow_%03d'%i,.95),220,3100)
    shot('CottonPress'+('' if i==0 else '_'+str(i)),[(soft,0,.85),(snow,.018,.20)],.42,.105)
    shot('RiverStep'+('' if i==0 else '_'+str(i)),[(eq(liquid('slime_%02d'%(i+3),.83),80,2000),0,.65),(soft,.015,.6)],.62,.11)
for i in range(3):
    shot('CottonLift'+('' if i==0 else '_'+str(i)),[(rpg('cloth'+str(i+1),1.12),0,.65),(liquid('slime_%02d'%(i+8),1.3),.025,.14)],.40,.085)
    shot('CottonLand'+('' if i==0 else '_'+str(i)),[(impact('impactSoft_heavy_%03d'%i,.83),0,.55),(impact('footstep_snow_%03d'%i,.8),.02,.16)],.67,.145)
    shot('RiverLand'+('' if i==0 else '_'+str(i)),[(impact('impactSoft_heavy_%03d'%i,.85),0,.55),(eq(liquid('splash_%02d'%(i+3),.72),100,1500),.015,.3),(liquid('bubble_0'+str(i+1),.8),.12,.3)],.85,.145)
    shot('Swing'+('' if i==0 else '_'+str(i)),[(rpg('knifeSlice'+('' if i==0 else '2'),.74+i*.07),0,.55),(rpg('cloth'+str(i+1),1.12),.02,.4)],.5,.11)
    shot('Contact'+str(i),[(impact('impactPunch_medium_%03d'%i),0,.7),(eq(crunch(i+1,1.15),150,2800),.012,.20)],.47,.14)
    shot('Crunch'+str(i),[(eq(crunch(i+1,.92),100,3400),0,.7),(impact('impactWood_light_%03d'%i),.03,.15)],.62,.1)
    shot('SoftHit'+str(i),[(impact('impactSoft_medium_%03d'%i,.86),0,.7),(eq(liquid('slime_%02d'%(i+2),.91),120,1800),.015,.17)],.5,.115)
    shot('Metal'+str(i),[(impact('impactMetal_medium_%03d'%i,.84),0,.65)],.8,.10)
    shot('Pickup'+('' if i==0 else '_'+str(i)),[(ui('glass_00'+str(i+1),1.1),0,.5),(ui('confirmation_002',1.07),.07,.16)],.75,.11)

for i in range(2):
    shot('Hurt'+('' if i==0 else '_'+str(i)),[(impact('impactPunch_heavy_%03d'%i,.84),0,.6),(rpg('cloth'+str(i+2),.85),.01,.35)],.55,.12)
    shot('Dodge'+('' if i==0 else '_'+str(i)),[(rpg('cloth'+str(i+2),.9),0,.75),(rpg('knifeSlice',1.3),.025,.12)],.45,.09)
    shot('Click'+('' if i==0 else '_'+str(i)),[(ui('click_00'+str(i+1)),0,.6)],.13,.07)
shot('Warning',[(impact('impactMetal_light_000',.8),0,.4),(impact('impactMetal_light_001',1.1),.19,.2)],.62,.12)
shot('LowImpact',[(eq(impact('impactSoft_heavy_001',.57),45,500),0,.9)],.65,.14)
shot('MandalaBell',[(impact('impactBell_heavy_000',.64),0,.65),(eq(impact('impactMetal_heavy_002',.75),75,2600),.04,.09)],2.5,.10)
shot('SealImpact',[(eq(impact('impactSoft_heavy_003',.68),45,1500),0,.7),(impact('impactPlate_heavy_000',.75),.02,.18)],1.05,.145)
shot('HuangWarning',[(impact('impactMetal_light_002',.86),0,.4),(ui('glass_002',.8),.13,.2)],.72,.105)
shot('BajieWarning',[(rpg('beltHandle1',.75),0,.55),(impact('impactWood_medium_000',.7),.16,.25)],.72,.115)
shot('BajieCharge',[(rpg('cloth2',.7),0,.25),(impact('footstep_snow_002',.68),.08,.55),(impact('footstep_snow_003',.67),.27,.45),(impact('footstep_snow_001',.7),.47,.35)],.92,.13)
shot('RakeSwing',[(rpg('knifeSlice2',.67),0,.5),(impact('impactMetal_light_004',.82),.10,.3)],.8,.11)
shot('CandyCreak',[(eq(crunch(2,.62),200,1900),0,.4),(rpg('creak1',1.1),.04,.16)],1.6,.045)
shot('BubblePop',[(eq(liquid('bubble_01',.66),110,1800),0,.6)],1,.07)
shot('BossDefeat',[(eq(crunch(5,.65),90,2200),0,.25),(impact('impactSoft_heavy_004',.61),.05,.5)],1.2,.12)

# Keep the melody puzzle's original C4, E4, G4, C5 notes, with a soft glass attack.
for i,freq in enumerate([261.63,329.63,392,523.25]):
    t=np.arange(int(.8*RATE))/RATE
    tone=(np.sin(2*np.pi*freq*t)*np.exp(-t*5)+.28*np.sin(2*np.pi*freq*2.76*t)*np.exp(-t*11)+.10*np.sin(2*np.pi*freq*5.4*t)*np.exp(-t*19))*.20
    tone*=np.minimum(1,t/.009)
    shot('Melody'+str(i),[(tone,0,1),(eq(ui('glass_002',1.1),3000,8000),0,.06)],.8,.09)

manifest=[]
for rel,p in sorted(used.items()):
    target=ORIG/rel;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,target)
    manifest.append({'file':rel,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
for pack in {rel.split('/')[0] for rel in used}:
    meta=SRC/pack/'SOURCE.json';target=ORIG/pack/'SOURCE.json';shutil.copyfile(meta,target)
    for license in (SRC/pack).glob('*[Ll]icense*.txt'):shutil.copyfile(license,ORIG/pack/license.name)
    for license in (SRC/pack).glob('*[Ll]icense*.md'):shutil.copyfile(license,ORIG/pack/license.name)
(ORIG/'SelectedFiles.json').write_text(json.dumps(manifest,indent=2))
(OUT/'Authoring.json').write_text(json.dumps({'sampleRate':RATE,'outputs':outputs},indent=2))

# A quiet audit reel: environment, movement, pickups, confirmed hits and boss accents.
parts=[(np.asarray(riverbed),0,.7),(eq(cyclic(wind,18),110,2400),0,.2)]
for name,at in [('CottonPress',2),('CottonPress_1',2.6),('CottonLand',3.5),('RiverStep',5),('RiverStep_1',5.7),('Pickup',7),('Contact0',9),('Contact1',9.7),('HuangWarning',11),('MandalaBell',12),('BajieWarning',15),('RakeSwing',16)]:
    x,sr=sf.read(OUT/(name+'.wav'));parts.append((x,at,.9))
reel=fade(norm(mix(parts,18),.10,.8),.6,.8)
sf.write('.codex/island2-audio/audition.wav',reel,RATE,subtype='PCM_16')
print(json.dumps({'editedClips':len(outputs),'sourceFiles':len(used),'processedBytes':sum(p.stat().st_size for p in OUT.glob('*') if p.suffix in ['.wav','.ogg']),'maximumPeak':max(o['peak'] for o in outputs.values())}))
