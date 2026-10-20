"""Summarize native Editor ship motion and contact recordings without changing assets."""
import json
from pathlib import Path

ROOT = Path('.codex/ship-motion-20261020')


def summarize(label):
    folder = ROOT / label
    rows = json.loads((folder / 'motion.json').read_text(encoding='utf-8'))
    warm = [r for r in rows if r['clock'] > .4]
    def limits(key):
        return [min(r[key] for r in warm), max(r[key] for r in warm)]
    result = dict(
        frames=len(rows), duration=rows[-1]['clock'],
        pitchDegrees=limits('pitch'), rollDegrees=limits('roll'), heightMetres=limits('y'),
        leftSoleGapMetres=limits('leftGap'), rightSoleGapMetres=limits('rightGap'),
        missingDeckContacts=sum(not r['leftFound'] or not r['rightFound'] for r in warm),
        unintendedPassengerRelease=sum(not r['passengerEnabled'] for r in warm),
        maxPitchStepDegrees=max(abs(b['pitch']-a['pitch']) for a,b in zip(warm,warm[1:])),
        maxRollStepDegrees=max(abs(b['roll']-a['roll']) for a,b in zip(warm,warm[1:])),
    )
    assert result['missingDeckContacts'] == 0, result
    assert result['unintendedPassengerRelease'] == 0, result
    for key in ['leftGap','rightGap']:
        assert all(.033 < r[key] < .037 for r in warm), (label, key)
    if (folder / 'handoff.json').exists():
        begin = json.loads((folder / 'handoff-start.json').read_text(encoding='utf-8'))
        impact = json.loads((folder / 'handoff.json').read_text(encoding='utf-8'))
        assert begin['shipDisabled'] and begin['passengerDisabled'] and begin['impacted'], begin
        assert begin['positionJump'] == 0 and begin['rotationJump'] < .001, begin
        assert not any(r['shipEnabled'] or r['passengerEnabled'] for r in impact)
        assert any(not r['shipActive'] for r in impact)
        assert impact[-1]['stage'] == 'Tutorial', impact[-1]
        result['handoff'] = dict(begin=begin, sampledFrames=len(impact), finalStage=impact[-1]['stage'],
                                  sailingResumed=False, fractured=True)
    return result


summary = {label: summarize(label) for label in ['sailing-before', 'sailing-after', 'low-rate-after']}
summary['contracts'] = json.loads((ROOT / 'contracts.json').read_text(encoding='utf-8'))
(ROOT / 'summary.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(summary, ensure_ascii=False, indent=2))
