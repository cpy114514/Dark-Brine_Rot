"""Restore the unchanged tracked ocean source and reapply the reviewed bow-water change."""
from pathlib import Path
import subprocess
path=Path('Assets/Ocean/ProceduralOcean.shader')
source=subprocess.check_output(['git','show','HEAD:Assets/Ocean/ProceduralOcean.shader']).decode('utf-8-sig').replace('\r\n','\n')
assert source.startswith('Shader ') and len(source)>15000
function='''            float BowLift(float2 p)
            {
                if (_ShipWakeMode < 0.5 || _WakeDirection.z < 0.01) return 0;
                float2 relative = p - _WakeHull.xy;
                float halfLength = max(_WakeHull.z, 1.0);
                if (dot(relative, relative) > halfLength * halfLength * 4.0) return 0;
                float fromBow = halfLength - dot(relative, _WakeDirection.xy);
                float across = abs(dot(relative, float2(-_WakeDirection.y, _WakeDirection.x)));
                float spread = max(0.35, fromBow * 0.24);
                float ridge = across - spread;
                float envelope = smoothstep(-1.0, 1.5, fromBow)
                    * (1.0 - smoothstep(halfLength * 0.65, halfLength * 1.4, fromBow));
                return _WakeDirection.z * envelope *
                    (0.28 * exp(-ridge * ridge / 2.8) - 0.055 * exp(-(ridge - 2.2) * (ridge - 2.2) / 5.0));
            }

'''
source=source.replace('            Varyings vert(Attributes input)',function+'            Varyings vert(Attributes input)',1)
source=source.replace('                output.positionWS = basePosition + displacement;','''                output.positionWS = basePosition + displacement;
                float bow = BowLift(output.positionWS.xz);
                float2 bowSlope = float2(BowLift(output.positionWS.xz + float2(0.4,0)) - bow,
                    BowLift(output.positionWS.xz + float2(0,0.4)) - bow) / 0.4;
                output.positionWS.y += bow;
                waveNormal = normalize(waveNormal + float3(-bowSlope.x,0,-bowSlope.y));''',1)
assert source.count('HLSLPROGRAM')==source.count('ENDHLSL')==1 and 'float BowLift' in source and len(source)>20000
path.write_bytes(source.replace('\n','\r\n').encode('utf-8'))
print('Complete shader restored with consistent CRLF:',len(source),'characters')
