"""Reproducible energy/throughput reference for the first cold-storage process."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
layout = json.loads((ROOT / 'docs/Pruefungen/Eislager-Slot01-Auslegung-2026-10-25.json').read_text(encoding='utf-8-sig'))
drone = json.loads((ROOT / 'docs/Pruefungen/Drohnen-90-Prozent-Rechnung-2026-10-25.json').read_text(encoding='utf-8-sig'))
load = drone['rawLoadAt80PercentWaterKg']
full = next(row for row in layout['capacityScenarios'] if row['solidPackingFraction'] == .75)

# Declared engineering reference, not a material-specific thermal solver.
# cp_ice uses a constant upper approximation, so sensible heating is conservative.
initial_k = 150
melting_k = 273.15
target_c = 5
cp_ice_kj_kg_k = 2.1
fusion_kj_kg = 333.5
cp_water_kj_kg_k = 4.18
ideal_kwh_liter = (cp_ice_kj_kg_k * (melting_k-initial_k)
                   + fusion_kj_kg + cp_water_kj_kg_k * target_c) / 3600
output = {
    'date':'2026-10-25',
    'scope':'First game process reference. Parameters are sizing assumptions; no full temperature, pressure, purity or heat rejection certification.',
    'coldVolumeM3':layout['proposal']['operationalFillVolumeM3'],
    'rawStorage80PercentWaterKg':full['rawMixtureKg'],
    'waterPotentialLiters':full['rawMixturePotentialWaterLitersApprox'],
    'fullDrone80PercentWaterKg':load,
    'equivalentFullLoads':full['rawMixtureKg'] / load,
    'process':{
        'crusherKw':.2,'crusherKwhPerKg':.0025,'crusherKgPerHour':80,
        'compactorKw':.2,'compactorKwhPerKg':.0025,'compactorKgPerHour':80,
        'refinerKw':1.5,'refinerKwhPerLiter':.2,'refinerLitersPerHour':7.5,
        'mechanicalEnergyPerFullLoadKwh':load*.005,
        'coldThroughputHoursPerFullLoad':load/80,
        'fullLoadWaterPotentialLiters':load*.8,
        'allFullLoadWaterRefiningKwh':load*.8*.2,
        'allFullLoadWaterRefiningHours':load*.8/7.5,
        'first180LitersRefiningKwh':180*.2,
        'first180LitersRefiningHours':180/7.5,
        'note':'Crusher and compactor operate as separate pipeline stages. Initial fill, source availability, power, valves and full destinations change elapsed time.'
    },
    'thermalReference':{
        'assumedInitialIceK':initial_k,'meltingK':melting_k,'targetLiquidC':target_c,
        'constantIceCpKjKgK':cp_ice_kj_kg_k,'fusionKjKg':fusion_kj_kg,
        'waterCpKjKgK':cp_water_kj_kg_k,'idealKwhPerLiter':ideal_kwh_liter,
        'adoptedKwhPerLiter':.2,'marginRelativeToReference':.2/ideal_kwh_liter-1,
        'limits':'Rest material heating, separation, pumps and losses share an assumed aggregate margin. Unknown material is treated as insoluble residue. No dissolved contaminant chemistry or sterilisation validation.'
    },
    'sources':[
        {'title':'NASA Aqua Factorem (cold grain separation concept)',
         'url':'https://www.nasa.gov/general/aqua-factorem-ultra-low-energy-lunar-water-extraction/',
         'supports':'Cold handling before phase change; no proof of this crusher or arbitrary asteroid ice.'},
        {'title':'WGMS Glossary of Glacier Mass Balance, Appendix B',
         'url':'https://wgms.ch/downloads/Cogley_etal_2011.pdf',
         'supports':'Search-indexed physical reference 333.5 kJ/kg fusion; direct PDF fetch timed out.'},
        {'title':'UK Space Agency ISRU Technology Review',
         'url':'https://www.gov.uk/government/publications/uk-in-situ-resource-utilisation-isru-technology-review/uk-isru-technology-review',
         'supports':'Extraction, storage, processing and purification are distinct technology functions; no hardware validation of our assumptions.'}
    ]
}
path = ROOT / 'docs/Pruefungen/Eisprozess-Rechnung-2026-10-25.json'
path.write_text(json.dumps(output, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print(json.dumps(output, ensure_ascii=False, indent=2))
