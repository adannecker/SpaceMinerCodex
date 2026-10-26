"""Reproducible engineering scenarios; reads source geometry, never edits Unity.

Run from any cwd: python tools/AssessStationEnergy.py
Unknown hardware remains explicit in Design/Energy/station-energy-reference-2026-10-26.json.
"""
import csv
import hashlib
import json
import math
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CONFIG = ROOT / "Design/Energy/station-energy-reference-2026-10-26.json"
OUTPUT = ROOT / "docs/Pruefungen/Stationsenergie-Rechnung-2026-10-26.json"
SOURCE_NAMES = ["StationVisual.cs", "RuinedWorld.cs", "WaterScenario.cs", "StationOperations.cs",
                "StationPowerReadModel.cs", "StationWork.cs", "StationIcePlant.cs", "StationInteriorMode.cs",
                "StationInteriorLayout.cs", "DroneAgent.cs", "StationOperations.Lighting.cs", "BinaryStarEnergy.cs"]

def length(v): return math.sqrt(sum(x*x for x in v))
def unit(v): return [x / length(v) for x in v]
def dot(a,b): return sum(x*y for x,y in zip(a,b))
def add(a,b): return [x+y for x,y in zip(a,b)]
def scale(v,s): return [x*s for x in v]
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def normal(x,z):
    # Unity Quaternion.Euler(x,0,z): Z, then X, then Y; applies to local up.
    x,z=map(math.radians,(x,z))
    return [-math.sin(z),math.cos(x)*math.cos(z),math.sin(x)*math.cos(z)]

def charge_hours(capacity,terminal_kw,start,end,storage_efficiency,hotel_kw=0):
    """Declared approximate CC/CV: full power to80%, linear taper to20% at100%.
    A displayed100% uses a finite BMS cutoff; not a manufacturer pack guarantee.
    """
    if not 0 <= start <= end <= 1: raise ValueError("state of charge outside0..1")
    cc=max(0,min(end,.8)-min(start,.8))*capacity/((terminal_kw-hotel_kw)*storage_efficiency)
    lo,hi=max(start,.8),max(end,.8)
    if hi<=lo:return cc
    # Above80%: P/Pnom=1-4*(soc-.8), ending at20% of nominal.
    cv=capacity/(4*terminal_kw*storage_efficiency)*math.log((terminal_kw*(1-4*(lo-.8))-hotel_kw)/(terminal_kw*(1-4*(hi-.8))-hotel_kw))
    return cc+cv

def assess(c,sources):
    vis,world=sources['StationVisual.cs'],sources['RuinedWorld.cs']
    # Fail loudly on changed design rather than silently retaining stale dimensions.
    expected={
        'StationVisual.cs':['wing.localScale = new Vector3(2, 1, 2)', 'new Vector3(11.4f,0.16f,10.7f)',
                           'x<7;x++) for(int z=0;z<5;z++)', 'new Vector3(1.43f,0.08f,1.93f)',
                           'side==0 && x==1 && z==3', 'side==1 && x>=5 && z==0',
                           'side==1 && x==3 && z==2', 'side == 0 ? 6f : -11f', 'side == 0 ? -3f : 5f'],
        'RuinedWorld.cs':['AstronomicalUnit = 1800f','MoonDistance = 18f','StationDistance = MoonDistance * 5',
                         'new Vector3(-.25f,.02f,1)', 'new Vector3(240,300,120)', 'Mathf.Cos(-42.746f*Mathf.Deg2Rad)']}
    for filename,needles in expected.items():
        for needle in needles:
            if needle not in sources[filename]:raise ValueError(f"Geometry changed; reassess {filename}: {needle}")
    home=scale(unit([-.25,.02,1]),90)
    phase=math.radians(-42.746)
    sun=add(home,scale([math.cos(phase),0,math.sin(phase)],-1800))
    companion=add(sun,[240,300,120])
    astronomy=c['astronomy_assumptions']; pv=c['solar']; e=c['electrical']; d=c['drones']
    if astronomy['physical_mapping_approved']:
        for ident in ('primary','companion'):
            pattern=r'new StarEnergyView\("sun-'+ident+r'","[^"]+",([\d.]+),([\d.]+),'
            match=re.search(pattern,sources['BinaryStarEnergy.cs'])
            if not match or float(match[1])!=astronomy[ident+'_temperature_k'] or float(match[2])!=astronomy[ident+'_radius_solar']:
                raise ValueError('Configured star and runtime definition differ: '+ident)
            expected=astronomy[ident+'_radius_solar']**2*(astronomy[ident+'_temperature_k']/5772)**4
            if abs(expected-astronomy[ident+'_luminosity_solar'])>1e-12:raise ValueError('Luminosity must follow R²T⁴')
    stars=[{'id':'primary','display_position_km':sun,'luminosity_solar':astronomy['primary_luminosity_solar']},
           {'id':'companion','display_position_km':companion,'luminosity_solar':astronomy['companion_luminosity_solar']}]
    for star in stars:
        star['direction']=unit(star['display_position_km'])
        star['display_distance_km']=length(star['display_position_km'])
        star['literal_display_distance_in_au']=star['display_distance_km']/149597870.7
        star['distance_au_assumed']=star['display_distance_km']/1800*astronomy['display_au_equals_physical_au']
        star['irradiance_w_m2']=pv['irradiance_w_m2_at_1_au']*star['luminosity_solar']/star['distance_au_assumed']**2
    delta=math.degrees(math.acos(dot(stars[0]['direction'],stars[1]['direction'])))
    weighted=add(scale(stars[0]['direction'],stars[0]['irradiance_w_m2']),scale(stars[1]['direction'],stars[1]['irradiance_w_m2']))
    optimum=unit(weighted)  # Valid here: both nearby stars remain on the front side.
    panels=[]
    for ident,label,count,x,z,health in [('solar-a','Right',32,-11,5,.65),('solar-b','Left',34,6,-3,.81)]:
        n=normal(x,z);area=count*1.43*1.93*4
        factor=area*pv['cell_efficiency']*pv['net_system_factor']*pv['remaining_cell_factor']/1000
        contributions=[{'star':s['id'],'angle_deg':math.degrees(math.acos(max(-1,min(1,dot(n,s['direction']))))),
                        'incidence':max(0,dot(n,s['direction'])), 'output_kw':factor*s['irradiance_w_m2']*max(0,dot(n,s['direction']))} for s in stars]
        panels.append({'id':ident,'side':label,'frame_m':[22.8,21.4],'frame_area_m2':22.8*21.4,
                       'remaining_tiles':count,'original_tiles':35,'tile_area_m2':1.43*1.93*4,
                       'remaining_pv_area_m2':area,'intact_original_pv_area_m2':35*1.43*1.93*4,
                       'normal':n,'legacy_condition':health,'contributions':contributions,
                       'static_output_kw':sum(s['output_kw'] for s in contributions),
                       'optimal_both_suns_kw':factor*sum(s['irradiance_w_m2']*max(0,dot(optimum,s['direction'])) for s in stars),
                       'single_sun_1au_face_on_kw':area*pv['irradiance_w_m2_at_1_au']*.001*pv['cell_efficiency']*pv['net_system_factor']})
        usable=min(area,35*1.43*1.93*4*health)
        panels[-1]['electrically_usable_area_m2']=usable
        panels[-1]['physical_unshaded_connected_potential_kw']=panels[-1]['static_output_kw']*usable/area
    solar_sensitivity=[]
    for lum in astronomy['companion_sensitivity_solar']:
        current=sum(p['contributions'][0]['output_kw']+p['contributions'][1]['output_kw']*lum/stars[1]['luminosity_solar'] for p in panels)
        w=add(scale(stars[0]['direction'],stars[0]['irradiance_w_m2']),scale(stars[1]['direction'],stars[1]['irradiance_w_m2']*lum/stars[1]['luminosity_solar']))
        best=sum(p['remaining_pv_area_m2'] for p in panels)*pv['cell_efficiency']*pv['net_system_factor']*length(w)/1000
        solar_sensitivity.append({'companion_luminosity_solar':lum,'static_kw':current,'both_wings_independently_aligned_kw':best})
    panel_vector=[0,0,0]
    for p in panels: panel_vector=add(panel_vector,scale(p['normal'],p['remaining_pv_area_m2']))
    # A single rigid station rotation preserves the relative wing tilts.
    rigid_best=length(panel_vector)*length(weighted)*pv['cell_efficiency']*pv['net_system_factor']/1000
    profiles={p['id']:p for p in c['technology_profiles']}
    stationary=[p for p in profiles if p not in ('ice','tool')]
    idle=sum(profiles[p]['standby_kw'] for p in stationary)+sum(d[k]['parked_kw']/e['charger_efficiency'] for k in ('exterior','interior'))
    active=sum(profiles[p]['active_kw'] if p not in ('printer','sorting','food') else profiles[p]['standby_kw'] for p in stationary)
    water=active+profiles['ice']['active_kw']
    charging=sum(d[k]['charger_output_kw']/e['charger_efficiency'] for k in ('exterior','interior'))
    def load(name,kw):return {'name':name,'consumer_bus_kw':kw,'generation_required_kw':kw/e['distribution_efficiency']}
    cases=[load('Quiet/parked; daily-average oxygen',idle),load('Occupied; raw ice process running',water),
           load('Raw ice plus both fast chargers',water+charging),
           load('Raw ice, both chargers and full6kW oxygen batch',water+charging-profiles['oxygen']['active_kw']+6),
           load('Above plus printer8.5kW,sorting0.8kW,food1.2kW and scanner1.2kW',water+charging-profiles['oxygen']['active_kw']+6
                +sum(profiles[p]['peak_kw']-profiles[p]['standby_kw'] for p in ('printer','scanner'))
                +sum(profiles[p]['active_kw']-profiles[p]['standby_kw'] for p in ('sorting','food')))]
    charging_cases={}
    for key in ('exterior','interior'):
        capacity=d[key].get('battery_kwh',d[key].get('proposed_battery_kwh'))
        power=d[key]['charger_output_kw']
        hotel=d[key]['parked_kw']
        tcc=charge_hours(capacity,power,.1,.8,e['battery_storage_efficiency'],hotel)
        charging_cases[key]={'capacity_kwh':capacity,'charger_output_kw':power,'station_bus_kw':power/e['charger_efficiency'],
                             'hotel_kw':hotel,'stored_kw_cc':(power-hotel)*e['battery_storage_efficiency'],
                             'minutes_10_80':tcc*60,
                             'minutes_10_100':charge_hours(capacity,power,.1,1,e['battery_storage_efficiency'],hotel)*60,
                             'energy_bus_10_80_kwh':tcc*power/e['charger_efficiency']}
    # Sustainability example: repeated complete sorties/work-and-charge cycles.
    ext=d['exterior'];arrival=1-ext['sortie_energy_kwh']/ext['battery_kwh']
    ext_charge=charge_hours(16,12,arrival,1,e['battery_storage_efficiency'],ext['parked_kw'])
    ext_energy=(ext['sortie_energy_kwh']/e['battery_storage_efficiency']+ext['parked_kw']*ext_charge)/e['charger_efficiency']
    int_work=4*.7/3;int_charge=charging_cases['interior']['minutes_10_80']/60
    ext_avg=ext_energy/(ext['sortie_minutes']/60+ext_charge)
    int_avg=charging_cases['interior']['energy_bus_10_80_kwh']/(int_work+int_charge)
    growth=[{'crew':crew,'exterior_drones':ne,'interior_drones':ni,
             'continuous_cycling_avg_generation_kw':(water+max(0,crew-1)*.4+ne*ext_avg+ni*int_avg)/e['distribution_efficiency'],
             'note':'Continuous work/charge cycles with enough docks; not all charging simultaneously, not a normal quest-day prediction;0.4kW extra/person allowance excludes new habitats.'}
            for crew,ne,ni in ((1,1,1),(3,3,3),(6,10,10))]
    i=d['interior'];mu0=4*math.pi*1e-7
    b=math.sqrt(2*mu0*i['proposed_mass_kg']*i['gravity_m_s2']/(i['pole_count']*i['pole_area_m2']))
    ampturns=b*i['gap_m']/mu0
    current=ampturns/i['turns_per_pole']
    resistance=i['copper_resistivity_ohm_m']*i['turns_per_pole']*i['mean_turn_length_m']/(i['wire_cross_section_mm2']*1e-6)
    copper_w=i['pole_count']*current**2*resistance
    thermal=c['thermal']; react=c['reactor'];sig=5.670374419e-8
    heat_leak=thermal['cold_store_area_m2_assumed']*thermal['insulation_u_w_m2_k_assumed']*(thermal['cabin_k']-thermal['cold_store_k_assumed'])
    waste=react['proposed_net_max_kwe']*(1/react['net_thermal_efficiency']-1)
    worst_heat=waste+react['proposed_net_max_kwe']
    radiators=[{'temperature_k':temp,'reactor_waste_emitting_area_m2':waste*1000/(thermal['radiator_emissivity']*sig*(temp**4-thermal['background_k']**4)),
               'all_power_dissipated_locally_emitting_area_m2':worst_heat*1000/(thermal['radiator_emissivity']*sig*(temp**4-thermal['background_k']**4))} for temp in (350,400,450)]
    backup=[{'name':s['name'],'full_to20percent_hours':e['station_capacity_kwh']*.8/s['generation_required_kw'],
             'current50_to20percent_hours':(e['station_start_kwh']-e['station_capacity_kwh']*.2)/s['generation_required_kw'],
             'legacy8kw_discharge_can_supply':s['generation_required_kw']<=8} for s in cases[:3]]
    # Deterministic physics checks, independent limiting cases and conservation.
    checks=0
    def require(value,message):
        nonlocal checks
        if not value:raise AssertionError(message)
        checks+=1
    require(abs(length(optimum)-1)<1e-12,'optimal normal unit length')
    require(abs(sum(p['remaining_pv_area_m2'] for p in panels)-728.6136)<1e-6,'66 actual remaining tiles')
    require(abs(math.cos(math.radians(60))-.5)<1e-12,'60degrees from normal gives50%')
    require(max(0,math.cos(math.pi))==0,'backside giveszero')
    require(abs((1/2**2)-.25)<1e-12,'twice distance givesquarter')
    for p in panels:
        require(p['static_output_kw']<=p['optimal_both_suns_kw'],'orientation cannot exceed optimum')
        require(0<=p['physical_unshaded_connected_potential_kw']<=p['static_output_kw'],'electrical and geometric loss cannot add energy')
        require(abs(length(p['normal'])-1)<1e-12,'panel normal unit length')
    for case in charging_cases.values():
        require(case['station_bus_kw']>case['charger_output_kw']>case['stored_kw_cc'],'charging losses counted')
        require(case['minutes_10_100']>case['minutes_10_80'],'full charge slower than partial charge')
        require(abs((case['energy_bus_10_80_kwh']*e['charger_efficiency']-case['hotel_kw']*case['minutes_10_80']/60)*e['battery_storage_efficiency']-.7*case['capacity_kwh'])<1e-10,'charge and hotel energy conserved')
    require(abs(react['proposed_net_max_kwe']/react['net_thermal_efficiency']-waste-react['proposed_net_max_kwe'])<1e-10,'reactor energy conserved')
    require(cases[-1]['generation_required_kw']>react['proposed_net_max_kwe'],'all peak processes require scheduling')
    require(cases[2]['generation_required_kw']>8,'fast charging cannot be supplied by legacy8kW battery alone')
    require(abs(profiles['oxygen']['active_kw']-.84*55.2/8/24)<1e-12,'oxygen stoichiometry and daily duty')
    require(abs(charge_hours(16,12,0,.8,.98)+charge_hours(16,12,.8,1,.98)-charge_hours(16,12,0,1,.98))<1e-12,'charge intervals additive')
    return {'scope':'Analytical calculation. Binary-star definitions integrated; see separate player verification. Other load/reactor/drone figures remain proposals.', 'date':c['date'],
            'solar_model_defined':astronomy['physical_mapping_approved'] and astronomy['stellar_luminosities_known'],
            'physical_unshaded_connected_potential_kw':sum(p['physical_unshaded_connected_potential_kw'] for p in panels),
            'solar_status':'Physical star definitions now fixed. Analytical values omit sampled scene occlusion and network isolation; actual runtime output is reported by the shared game read model.',
            'assumptions':c,'stars':stars,'sun_separation_deg':delta,'optimal_normal':optimum,'panels':panels,
            'solar_existing_geometry_undamaged_static_kw':sum(p['static_output_kw'] for p in panels),
            'solar_existing_geometry_best_aligned_kw':sum(p['optimal_both_suns_kw'] for p in panels),
            'solar_best_rigid_rotation_geometry_kw':rigid_best,
            'solar_current_hardware_best_aligned_kw':sum(p['optimal_both_suns_kw']*p['electrically_usable_area_m2']/p['remaining_pv_area_m2'] for p in panels),
            'solar_sensitivity':solar_sensitivity,'load_cases':cases,'charging':charging_cases,
            'growth':growth,'continuous_cycle_average_bus_kw':{'exterior':ext_avg,'interior':int_avg},
            'station_backup':backup,'maglev_example':{'flux_tesla':b,'ampere_turns_per_pole':ampturns,'amps_per_pole':current,'ohms_per_pole':resistance,'copper_loss_kw':copper_w/1000,
                         'copper_only_loss_at_10mm_kw':copper_w/1000*(.01/.08)**2,'usable70percent_hours_moving':4*.7/3,'note':'Illustrative assumed geometry; fringing, second return gap, temperature and magnetic technology are unresolved'},
            'thermal':{'cold_store_heat_leak_w':heat_leak,'cold_store_cooling_electric_kw':heat_leak/thermal['cryocooler_cop_assumed']/1000,
                       'reactor_full_net_kwe':40,'reactor_full_thermal_kw':160,'reactor_waste_kw':waste,'worst_local_heat_kw':worst_heat,'radiators':radiators},
            'checks':{'passed':True,'count':checks}}

def main():
    config=json.loads(CONFIG.read_text(encoding='utf-8'))
    paths={n:ROOT/'Assets/SpaceMiner/Scripts'/n for n in SOURCE_NAMES}
    sources={n:p.read_text(encoding='utf-8-sig') for n,p in paths.items()}
    result=assess(config,sources)
    result['source_hashes']={str(p.relative_to(ROOT)).replace('\\','/'):digest(p) for p in paths.values()}
    result['config_sha256']=digest(CONFIG)
    OUTPUT.parent.mkdir(parents=True,exist_ok=True)
    OUTPUT.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    table=ROOT/'docs/Pruefungen/Stationsenergie-Technologien-2026-10-26.csv'
    with table.open('w',encoding='utf-8-sig',newline='') as f:
        writer=csv.DictWriter(f,fieldnames=list(config['technology_profiles'][0]));writer.writeheader();writer.writerows(config['technology_profiles'])
    print(json.dumps({k:result[k] for k in ('solar_model_defined','physical_unshaded_connected_potential_kw','solar_existing_geometry_best_aligned_kw','solar_current_hardware_best_aligned_kw','sun_separation_deg','load_cases','charging','maglev_example','station_backup','checks')},ensure_ascii=False,indent=2))

if __name__=='__main__':main()
