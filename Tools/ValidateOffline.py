from pathlib import Path
import re,json,struct,math
import numpy as np,yaml
from tree_sitter import Language,Parser
import tree_sitter_c_sharp
P=Path(__file__).resolve().parents[1]
parser=Parser(Language(tree_sitter_c_sharp.language()))
checks=[]
def require(condition,message):
 if not condition:raise AssertionError(message)
 checks.append(message)
for p in (P/'Assets').rglob('*.cs'):
 tree=parser.parse(p.read_bytes());require(not tree.root_node.has_error,'C# syntax parsed: '+p.name)
# Unity's !u! tags need stripping for an independent YAML parser.
def unity_docs(p):
 s=p.read_text();s=re.sub(r'^%.*\n','',s,flags=re.M);s=re.sub(r'^--- !u!\d+ &(\d+)','---\n__id: \\1',s,flags=re.M)
 return list(yaml.safe_load_all(s))
assets=[]
for p in P.rglob('*'):
 if p.suffix in ('.asset','.unity','.mat'):
  unity_docs(p);assets.append(str(p.relative_to(P)))
require(True,f'Unity YAML parsed: {len(assets)} files')
guids={}
for p in (P/'Assets').rglob('*.meta'):
 g=re.search(r'^guid: ([0-9a-f]{32})',p.read_text(),re.M).group(1)
 require(g not in guids,'Unique GUID: '+p.name);guids[g]=p
external={'bf2edee5c58d82540a51f03df9d42094','de640fe3d0db1804a85f9fc8f5cadab6','00000000000000000000000000000000'}
for p in [P/a for a in assets]:
 for g in re.findall(r'guid: ([0-9a-f]{32})',p.read_text()):require(g in guids or g in external,'Resolved GUID reference: '+p.name)
scene=P/'Assets/Scenes/CanyonPrototype.unity';docs=unity_docs(scene);ids={d['__id'] for d in docs};require(len(ids)==len(docs),'Scene fileIDs are unique')
for m in re.finditer(r'\{fileID: (\d+)\}',scene.read_text()):
 i=int(m.group(1));require(i==0 or i in ids,'Resolved local scene reference')
for p in (P/'Assets/Art/Meshes').glob('*.asset'):
 mesh=unity_docs(p)[0]['Mesh'];vd=mesh['m_VertexData'];raw=bytes.fromhex(str(vd['_typelessdata']));v=np.array(struct.unpack('<'+'f'*(len(raw)//4),raw)).reshape(-1,6)
 require(len(raw)==vd['m_DataSize']==vd['m_VertexCount']*24,'Vertex buffer size: '+p.stem)
 require(np.allclose(np.linalg.norm(v[:,3:],axis=1),1,atol=1e-5),'Unit normals: '+p.stem)
 b=mesh['m_IndexBuffer'];index=struct.unpack('<'+'H'*(len(str(b))//4),bytes.fromhex(str(b)));require(max(index)<len(v),'Mesh indices in bounds: '+p.stem)
 require(np.all(np.isfinite(v)),'Finite geometry: '+p.stem)
 # All face normals on the convex base meshes should point outwards.
 tri=v[:,:3].reshape(-1,3,3);n=v[:,3:].reshape(-1,3,3)[:,0]
 require(np.all(np.sum(tri.mean(axis=1)*n,axis=1)>0),'Outward face winding: '+p.stem)
# Reachability: every ordinary neighboring hold lies within the specified supported reach.
inv=json.loads((P/'Documentation/SceneInventory.json').read_text());h=inv['holds'];gaps=[]
for a,b in zip(h,h[1:]):
 d=math.hypot(b['arc']-a['arc'],b['height']-a['height'])
 if d>1.5:gaps.append((a,b))
 else:require(d>0,'Distinct neighboring holds')
require(len(gaps)==2,'Exactly two authored momentum gaps')
# Independent ballistic feasibility search, including reachable states from a driven pendulum.
# Pump in the direction of movement, with a 0.3s initial kick to start the pendulum.
states=[];theta=omega=0.;dt=.02
for i in range(1200):
 pump=1 if i<15 or omega>=0 else -1
 omega=max(-4,min(4,omega+(-9.8/1.15*math.sin(theta)+pump*7.2-.65*omega)*dt))
 theta+=omega*dt
 if abs(theta)>1.1:theta=math.copysign(1.1,theta);omega*= -.25
 if i>50:states.append((theta,omega))
report=[]
for a,b in gaps:
 delta=np.array([b['arc']-a['arc'],b['height']-a['height']])
 best=(999,None)
 for theta,omega in states[::3]:
  body=np.array([1.15*math.sin(theta),-1.15*math.cos(theta)])
  carry=np.array([math.cos(theta),math.sin(theta)])*1.15*omega*.7
  for angle in np.linspace(.45,1.5,100):
   velocity=carry+np.array([math.cos(angle)*4.7,math.sin(angle)*6.1])
   times=np.arange(.12,1.42,.02)
   pos=body+velocity*times[:,None]+np.array([0,-4.9])*times[:,None]**2+np.array([0,1.15])
   distances=np.linalg.norm(pos-delta,axis=1);j=int(np.argmin(distances));d=distances[j]
   if d<best[0]:best=(d,{'swingAngle':theta,'angularSpeed':omega,'aimAngleDegrees':angle*180/math.pi,'catchTime':float(times[j])})
 require(best[0]<.48,f'Momentum gap {a["id"]}->{b["id"]} has a catchable trajectory')
 report.append({'from':a['id'],'to':b['id'],'closestHandDistance':float(best[0]),'example':best[1]})
require(h[-1]['arc']/6.6>5.4,'Route travels more than 300 degrees around the cliff')
summary={'result':'PASS','checks':len(checks),'csharpFiles':len(list((P/'Assets').rglob('*.cs'))),'yamlFiles':len(assets),'gameObjects':sum('GameObject' in d for d in docs),'holds':len(h),'momentumGapFeasibility':report,'limitations':['No Unity Editor available: no Unity compilation, shader compilation, import, Play Mode, Android build or device profiling performed.','Numerical checks establish geometric/ballistic feasibility, not usability or playability.']}
(P/'Documentation/StaticValidation.json').write_text(json.dumps(summary,indent=2))
print(json.dumps(summary,indent=2))
