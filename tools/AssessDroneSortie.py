"""Independent idealized sortie calculation for the adopted 90 percent load target."""
import json
import math
import re
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
agent=(ROOT/'Assets/SpaceMiner/Scripts/DroneAgent.cs').read_text(encoding='utf-8-sig')
profile=(ROOT/'Assets/SpaceMiner/Scripts/DroneCargoProfile.cs').read_text(encoding='utf-8-sig')
def value(text,name):
    return float(re.search(r'\b'+re.escape(name)+r'\s*=\s*([.\d]+)f',text).group(1))
p={name:value(agent,name) for name in ['DryMassKg','FuelCapacityLiters','BatteryCapacityKwh',
    'CargoCapacityKg','CruiseSpeed','ThrustNewtons','ExhaustSpeed','HeatingKwhPerLiter',
    'MiningRateKgPerSecond','MiningPowerKw','OnboardPowerKw','ReserveFraction']}
hold={name:value(profile,name) for name in ['DefaultUsableVolumeM3','DefaultTargetFillFraction',
    'DefaultPackingFraction','DefaultIceDensityKgM3','DefaultRestDensityKgM3']}
def capacity(water):
    density=1/(water/hold['DefaultIceDensityKgM3']+(1-water)/hold['DefaultRestDensityKgM3'])
    return min(p['CargoCapacityKg'],density*hold['DefaultUsableVolumeM3']*hold['DefaultTargetFillFraction']*hold['DefaultPackingFraction'])
def leg_time(distance,speed,mass):
    a=p['ThrustNewtons']/mass
    peak=min(speed,math.sqrt(a*distance))
    return 2*peak/a+max(0,distance-peak*peak/a)/peak
def sortie(load,speed):
    mass=p['DryMassKg']+p['FuelCapacityLiters'];used=0
    for burn in range(4):
        if burn==2:mass+=load
        lost=mass*(1-math.exp(-speed/p['ExhaustSpeed']))
        mass-=lost;used+=lost
    seconds=leg_time(10000,speed,p['DryMassKg']+p['FuelCapacityLiters'])+leg_time(10000,speed,p['DryMassKg']+p['FuelCapacityLiters']+load)
    mine_seconds=load/p['MiningRateKgPerSecond']
    prop_kwh=used*p['HeatingKwhPerLiter']
    tool_kwh=mine_seconds*p['MiningPowerKw']/3600
    board_kwh=(seconds+mine_seconds)*p['OnboardPowerKw']/3600
    required=prop_kwh+tool_kwh+board_kwh
    return {'cruiseMps':speed,'waterUsedLiters':used,'propulsionKwh':prop_kwh,
        'miningSeconds':mine_seconds,'miningToolKwh':tool_kwh,'onboardKwh':board_kwh,
        'totalConsumedKwh':required,'returnBatteryKwh':p['BatteryCapacityKwh']-required,
        'returnWaterLiters':p['FuelCapacityLiters']-used,'flightMinutes':seconds/60,
        'withinWaterAndEnergyReserves':used<=p['FuelCapacityLiters']*(1-p['ReserveFraction']) and required<=p['BatteryCapacityKwh']*(1-p['ReserveFraction'])}
load=capacity(.8)
output={'date':'2026-10-25','scope':'Analytical four-burn 10 km each-way reference; actual campaign flight acceptance recorded separately.',
    'hardware':p,'cargoProfile':hold,'rawLoadAt80PercentWaterKg':load,
    'pureIceLoadKg':capacity(1),'rawLoadAt65PercentWaterKg':capacity(.65),
    'miningSpecificToolEnergyKwhPerTonne':1000*p['MiningPowerKw']/p['MiningRateKgPerSecond']/3600,
    'referenceSorties':[sortie(load,s) for s in [2,5]],
    'limits':['80/20 mixture uses assumed mineral density; geometry and packing are sizing assumptions.',
        'Heater 1.2 kWh/L and mechanical .5 kg/s at 4 kW are declared first game-sizing assumptions, not NASA-certified hardware.',
        'No additional dock, return-to-berth, avoidance, asteroid gravity or torque burns in this four-burn reference.',
        'Time uses conservative constant initial mass per leg. No spent water is replenished before the return burn.',
        'Station tank demand may limit actual cargo below the 90 percent maximum.']}
target=ROOT/'docs/Pruefungen/Drohnen-90-Prozent-Rechnung-2026-10-25.json'
target.write_text(json.dumps(output,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(output,ensure_ascii=False,indent=2))
