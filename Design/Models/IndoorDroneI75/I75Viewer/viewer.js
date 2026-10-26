import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { RoomEnvironment } from 'three/addons/environments/RoomEnvironment.js';

const stage=document.querySelector('#stage'),status=document.querySelector('#status');
const renderer=new THREE.WebGLRenderer({antialias:true});
renderer.setPixelRatio(Math.min(devicePixelRatio,1.75));
renderer.outputColorSpace=THREE.SRGBColorSpace;
renderer.toneMapping=THREE.ACESFilmicToneMapping;renderer.toneMappingExposure=1;
renderer.shadowMap.enabled=true;renderer.shadowMap.type=THREE.PCFSoftShadowMap;
stage.appendChild(renderer.domElement);renderer.domElement.setAttribute('aria-label','I75 3D-Ansicht');
const scene=new THREE.Scene();scene.background=new THREE.Color('#243440');
const camera=new THREE.PerspectiveCamera(37,1,.005,100);
const controls=new OrbitControls(camera,renderer.domElement);
controls.enableDamping=true;controls.dampingFactor=.075;controls.autoRotateSpeed=1.1;
controls.minDistance=.16;controls.maxDistance=5;
const pmrem=new THREE.PMREMGenerator(renderer),room=new RoomEnvironment();
scene.environment=pmrem.fromScene(room,.04).texture;room.dispose();pmrem.dispose();
const key=new THREE.DirectionalLight(0xe3f0ff,2.3);key.position.set(1.2,2,1.5);key.castShadow=true;
key.shadow.mapSize.set(1024,1024);Object.assign(key.shadow.camera,{left:-1,right:1,top:1,bottom:-1,near:.1,far:6});
key.shadow.bias=-.00015;scene.add(key);scene.add(new THREE.HemisphereLight(0xaec4d5,0x26303b,.6));
const floor=new THREE.Mesh(new THREE.PlaneGeometry(200,200),new THREE.MeshStandardMaterial({color:0x293a48,roughness:.78,metalness:.08}));
floor.rotation.x=-Math.PI/2;floor.receiveShadow=true;scene.add(floor);
const loader=new GLTFLoader();let current=null,sequence=0,distance=1.5,center=new THREE.Vector3(0,.2,0);
function size(){renderer.setSize(stage.clientWidth,stage.clientHeight);camera.aspect=stage.clientWidth/stage.clientHeight;camera.updateProjectionMatrix();}
addEventListener('resize',size);size();
function view(name='reset'){
  const directions={front:[0,.12,1],back:[0,.12,-1],side:[1,.12,0],top:[0,1,.001],bottom:[0,-1,.001],reset:[1.1,.65,1.3]};
  const direction=new THREE.Vector3(...directions[name]).normalize();camera.up.set(0,1,0);
  camera.position.copy(center).addScaledVector(direction,distance);controls.target.copy(center);controls.update();
}
function dispose(root){root.traverse(node=>{if(!node.isMesh)return;node.geometry.dispose();for(const material of [].concat(node.material)){for(const value of Object.values(material))if(value?.isTexture)value.dispose();material.dispose();}});}
function wireframe(){current?.traverse(node=>{if(node.isMesh)for(const material of [].concat(node.material))material.wireframe=document.querySelector('#wire').checked;});}
function load(variant){
  const request=++sequence;status.classList.remove('error');status.textContent=`${variant} wird geladen …`;
  loader.load(`../Surface-${variant}-2026-10-26/I75_V02_Surface_${variant}.glb`,gltf=>{
    if(request!==sequence){dispose(gltf.scene);return;}
    if(current){scene.remove(current);dispose(current);}current=gltf.scene;scene.add(current);
    let triangles=0;current.traverse(node=>{if(node.isMesh){node.castShadow=true;node.receiveShadow=true;triangles+=(node.geometry.index?.count??node.geometry.attributes.position.count)/3;}});
    const box=new THREE.Box3().setFromObject(current),sphere=box.getBoundingSphere(new THREE.Sphere());box.getCenter(center);
    floor.position.y=box.min.y-.004;
    distance=sphere.radius/Math.sin(THREE.MathUtils.degToRad(camera.fov/2))*1.15;
    // Keep enough room for the lower control panel in a narrow right sidebar.
    distance*=Math.max(1,1/camera.aspect*.90);controls.minDistance=sphere.radius*.55;controls.maxDistance=sphere.radius*16;
    view();wireframe();renderer.domElement.dataset.variant=variant;renderer.domElement.dataset.triangles=String(triangles);
    const lengthCm=box.getSize(new THREE.Vector3()).z*100;
    status.textContent=`${variant} · ${triangles.toLocaleString('de-DE')} Dreiecke · ${lengthCm.toLocaleString('de-DE',{maximumFractionDigits:1})} cm Länge`;
  },progress=>{if(request===sequence&&progress.total)status.textContent=`${variant} wird geladen · ${Math.round(progress.loaded/progress.total*100)} %`;},error=>{
    console.error(error);status.classList.add('error');status.textContent='Modell konnte nicht geladen werden. Lokalen Vorschauserver prüfen.';
  });
}
document.querySelector('#variant').addEventListener('change',event=>load(event.target.value));
document.querySelector('#reset').addEventListener('click',()=>{document.querySelector('#rotate').checked=false;controls.autoRotate=false;view();});
document.querySelector('#rotate').addEventListener('change',event=>controls.autoRotate=event.target.checked);
document.querySelector('#wire').addEventListener('change',wireframe);
document.querySelectorAll('[data-view]').forEach(button=>button.addEventListener('click',()=>view(button.dataset.view)));
renderer.setAnimationLoop(()=>{controls.update();renderer.render(scene,camera);renderer.domElement.dataset.camera=camera.position.toArray().map(x=>x.toFixed(4)).join(',');});
load('R03');
