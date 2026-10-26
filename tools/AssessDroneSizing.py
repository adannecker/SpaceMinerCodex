"""Read current defaults and calculate idealized sizing; never modifies game data."""
import json
import math
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
agent_text = (ROOT / 'Assets/SpaceMiner/Scripts/DroneAgent.cs').read_text(encoding='utf-8-sig')
cargo_text = (ROOT / 'Assets/SpaceMiner/Scripts/MaintenanceLaserVisual.cs').read_text(encoding='utf-8-sig')

def number(text, name):
    found = re.search(r'\b' + re.escape(name) + r'\s*=\s*([.\d]+)f', text)
    if not found:
        raise ValueError('Missing numeric default: ' + name)
    return float(found.group(1))

names = ['DryMassKg','FuelCapacityLiters','BatteryCapacityKwh','CargoCapacityKg',
         'CruiseSpeed','ThrustNewtons','ExhaustSpeed','HeatingKwhPerLiter',
         'MiningRateKgPerSecond','MiningPowerKw','OnboardPowerKw','ReserveFraction']
p = {name:number(agent_text,name) for name in names}
outer = [number(cargo_text,name) for name in ['CargoWidth','CargoDepth','CargoHeight']]
checked = [1.20,1.50,.875-.065]

def four_burns(cargo, speed):
    # Two burns outbound, pick up cargo, two burns inbound. Instantaneous burns
    # for propellant accounting only; flight time below includes acceleration.
    mass = p['DryMassKg'] + p['FuelCapacityLiters']
    water = 0.0
    for index in range(4):
        if index == 2:
            mass += cargo
        used = mass * (1 - math.exp(-speed/p['ExhaustSpeed']))
        mass -= used
        water += used
    return water

def leg_seconds(distance, speed, mass):
    acceleration = p['ThrustNewtons']/mass
    peak = min(speed, math.sqrt(acceleration*distance))
    return 2*peak/acceleration + max(0,distance-peak**2/acceleration)/peak

def max_cruise_with_reserve(cargo):
    dry = p['DryMassKg']
    fuel = p['FuelCapacityLiters']
    reserve = fuel*p['ReserveFraction']
    # Solve (dry+cargo+reserve)*q^2 - cargo*q - (dry+fuel)=0,
    # with q=exp(2*v/ve); cargo picked up at the turnaround stop.
    a = dry+cargo+reserve
    q = (cargo+math.sqrt(cargo*cargo+4*a*(dry+fuel)))/(2*a)
    return p['ExhaustSpeed']*.5*math.log(q)

scenarios = []
for cargo in [50,250,500]:
    for speed in [2,5]:
        water = four_burns(cargo,speed)
        seconds = leg_seconds(10_000,speed,p['DryMassKg']+p['FuelCapacityLiters'])
        seconds += leg_seconds(10_000,speed,p['DryMassKg']+p['FuelCapacityLiters']+cargo)
        scenarios.append({'pickupKg':cargo,'cruiseMps':speed,'waterLiters':water,
            'waterRemainingLiters':p['FuelCapacityLiters']-water,
            'withinTenPercentWaterReserve':water <= p['FuelCapacityLiters']*(1-p['ReserveFraction']),
            'propulsionKwhAtCurrentGameAssumption':water*p['HeatingKwhPerLiter'],
            'flightMinutesFor10kmEachWayApprox':seconds/60,
            'avionicsKwhForThatFlight':seconds*p['OnboardPowerKw']/3600,
            'maxCruiseWithTenPercentWaterReserveMps':max_cruise_with_reserve(cargo)})

output = {
    'date':'2026-10-25','scope':'Analytical audit of current source defaults; no Unity build/runtime test or game parameter change.',
    'defaults':p,
    'geometry':{'cargoOuterDimensionsM':outer,'cargoOuterBoxM3':math.prod(outer),
        'existingPackedGeometryCheckDimensionsM':checked,'existingCheckedBoxM3':math.prod(checked),
        'kgPerCheckedM3AtCurrentPayload':p['CargoCapacityKg']/math.prod(checked),
        'limit':'Checked rectangular envelope, not integrated usable interior volume. Model hull width normalized to 2 m.'},
    'ispSeconds':p['ExhaustSpeed']/9.80665,
    'fullFuelDeltaVEmptyMps':p['ExhaustSpeed']*math.log((p['DryMassKg']+p['FuelCapacityLiters'])/p['DryMassKg']),
    'fullFuelDeltaVLoaded50kgMps':p['ExhaustSpeed']*math.log((p['DryMassKg']+p['FuelCapacityLiters']+50)/(p['DryMassKg']+50)),
    'continuousThrustWaterKgPerSecond':p['ThrustNewtons']/p['ExhaustSpeed'],
    'continuousThrustWaterMinutes':p['FuelCapacityLiters']*p['ExhaustSpeed']/p['ThrustNewtons']/60,
    'miningMinutesToEmptyIdeal':60*p['BatteryCapacityKwh']/(p['MiningPowerKw']+p['OnboardPowerKw']),
    'miningMinutesToTenPercentReserveBeforeFlight':60*p['BatteryCapacityKwh']*(1-p['ReserveFraction'])/(p['MiningPowerKw']+p['OnboardPowerKw']),
    'full50kgMiningKwh':50/p['MiningRateKgPerSecond']*(p['MiningPowerKw']+p['OnboardPowerKw'])/3600,
    'onboardOnlyHoursIdeal':p['BatteryCapacityKwh']/p['OnboardPowerKw'],
    'chargeHoursAt2kwIdeal':p['BatteryCapacityKwh']/2,
    'latentHeatExampleKwhPerKgAtApprox100C':2257.5/3600,
    'example20CToApprox100CSteamKwhPerKg':(2674.9-83.9)/3600,
    'scenarios':scenarios,
    'limits':['Local straight-line co-moving rendezvous; no orbital drift or asteroid gravity.',
        'Four speed changes; no docking, turns, avoidance, extra waypoint stops, safety margin or reserve consumption.',
        'Times use conservative constant starting mass per leg; propellant uses sequential ideal rocket equation.',
        'Displayed scenarios beyond available water are infeasible; energy figures use unvalidated current 0.2 kWh/L.',
        '50 W onboard-only estimate is ideal continuous load; Ready state currently does not consume onboard energy.',
        'Thermal values are a reference case, not a universal vacuum engine operating point; no double counting of nozzle conversion energy.']
}
target = ROOT / 'docs/Pruefungen/Drohnen-Auslegung-Analyse-2026-10-25.json'
target.write_text(json.dumps(output,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(output,ensure_ascii=False,indent=2))
