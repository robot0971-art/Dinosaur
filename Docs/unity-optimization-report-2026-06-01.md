# Unity 최적화 리포트

프로젝트: `C:\Users\admin\Dino Game`  
작성일: 2026-06-01  
분석 방식: 정적 스캔 + Unity Editor 읽기 전용 점검  
정적 스캔 근거: `C:\Users\admin\Dino Game\unity-static-scan.json`

## 요약

| 영역 | 우선순위 | 요약 |
| --- | --- | --- |
| 텍스처 압축 | 높음 | 텍스처 import/source-size 관련 점검 항목이 216개 발견됨. 2K 지형, 스카이박스, VFX, 아틀라스 파일 중 큰 파일이 많음. |
| 메모리/로딩 | 높음 | `Resources` 아래 에셋이 56개 있음. 서드파티 데모/예제 에셋이 많아 빌드에 실수로 포함될 위험이 있음. |
| 드로우콜 | 중간 | 스캔된 씬/프리팹 기준 Renderer 3,232개, 고유 Material 참조 63개, LODGroup 0개가 확인됨. |
| 물리 | 중간 | Collider 1,965개, MeshCollider 731개가 확인됨. Unity 물리 검증에서도 모든 레이어가 서로 충돌하도록 설정되어 있다는 경고가 나옴. |
| 셰이더/GPU | 중간 | URP 사용 중. VFX 셰이더 중 Transparent, Multi-pass, Keyword가 많은 패턴이 있음. |
| 조명/그림자 | 중간 | Light 72개, ReflectionProbe 6개가 확인됨. URP PC 프로파일의 Shadow Distance는 35. |
| UI | 중간 | Canvas 35개가 확인됨. 게임플레이 씬에서 UI 분리 방식과 갱신 빈도를 점검해야 함. |
| 오디오 | 중간 | BGM과 게임오버 사운드의 Load Type/Compression 설정을 재검토할 필요가 있음. |
| 애니메이션/VFX | 중간 | SkinnedMeshRenderer 132개, ParticleSystem 127개, Animator 66개가 확인됨. |
| 빌드/플랫폼 | 중간 | Build Settings에는 씬 5개가 등록되어 있음. 프로젝트 전체에는 패키지/데모 씬을 포함해 씬 에셋 63개가 있음. |
| LOD/Occlusion/Terrain | 중간 | Terrain 7개, TerrainData 14개, LODGroup 0개가 확인됨. Occlusion Culling 적용 여부는 맵 구조별로 판단 필요. |
| 2D/네트워크/Addressables | 낮음 | 2D 패키지와 Sprite는 있으나 본 프로젝트는 3D 게임임. 네트워크와 Addressables는 발견되지 않음. |

## 수집한 근거

- 정적 스캔 파일: `unity-static-scan.json`
- Unity Editor에서 열린 씬: `Assets/Scenes/TitleScene.unity`
- 빌드 등록 씬: `TitleScene`, `GameScene`, `map4`, `map7`, `map10`
- URP 현재 설정: `PC_RPAsset`, Render Scale 1.0, HDR Off, MSAA x1, Shadow Distance 35, Cascade 2, Additional Lights 0
- 물리 검증: 현재 씬의 14개 오브젝트 스캔, 경고 1개. 모든 레이어가 모든 레이어와 충돌하도록 설정되어 있음
- Profiler 프레임 데이터: 게임플레이 상태에서는 아직 캡처하지 않음. 현재 타이틀/에디터 상태의 렌더링 통계는 보이는 지오메트리가 없어 0으로 반환됨. 실제 게임플레이 Play Mode 캡처가 추가로 필요함

## 우선 개선 항목

### 1. 대형 텍스처 Import 설정을 먼저 점검해야 함

- 영역: 텍스처 압축 / 메모리
- 근거: `Assets/Fantasy Skybox FREE/Scenes/Textures (Terrain)/Texture_Dirt_Diffuse.png` 20 MB, `ReflectionProbe-0.exr` 17.5 MB, `Assets/IslandMap/.../LPEP_TextureAtlas_TestCutout.tga` 16.8 MB, 그 외 2K VFX/지형 텍스처 다수
- 영향도: 높음
- 확신도: 높음
- 작업량: 낮음~중간
- 권장 작업: 게임플레이 맵에서 멀리 보이는 지형/장식 텍스처는 품질이 허용되는 범위에서 1024 이하로 제한한다. 월드 텍스처는 mipmap을 유지하고 플랫폼별 override를 설정한다. Desktop은 일반적으로 Color/Albedo에 BC7, Normal에 BC5를 사용하고 Android/iOS는 ASTC를 우선 검토한다. 사용하지 않는 데모 텍스처는 빌드에서 제외하거나 `Assets` 밖으로 이동한다.
- 검증 방법: import 변경 전후로 빌드 크기, 텍스처 메모리, 시각 품질을 Memory Profiler와 Frame Debugger에서 비교한다.

### 2. `Resources` 폴더 사용이 빌드 크기와 메모리 수명을 키울 수 있음

- 영역: 메모리 / 로딩
- 근거: 정적 스캔에서 `Resources` 아래 에셋 56개 발견. 런타임 코드에서는 `Assets/Scripts/Gameplay/Enemy/EnemySpawner.cs`가 `Area_star_ellow`를 `Resources.Load`로 로드하는 패턴이 확인됨
- 영향도: 높음
- 확신도: 높음
- 작업량: 중간
- 권장 작업: 정말 런타임 문자열 로딩이 필요한 에셋만 `Resources`에 남긴다. 패키지 데모/예제용 Resource는 빌드 경로 밖으로 옮긴다. 게임플레이에서 쓰는 이펙트나 프리팹은 가능하면 `[SerializeField]` 참조나 DI 등록으로 바꾸고, 맵/이펙트가 커지면 Addressables 도입을 검토한다.
- 검증 방법: 정리 전후로 한 번씩 빌드하고 `Editor.log`의 빌드 리포트 크기와 씬 전환 후 런타임 메모리를 비교한다.

### 3. 서드파티 데모 씬과 예제 에셋이 프로젝트에 많이 남아 있음

- 영역: 빌드/플랫폼 / 메모리
- 근거: 씬 에셋 63개 발견. 예: `Assets/TextMesh Pro/Examples & Extras/Scenes/...`, `Assets/Fantasy Skybox FREE/Scenes/...`, `Assets/Fog Particles/Demo.unity`, `Assets/IgniteCoders/Simple Water Shader/Demo.unity`
- 영향도: 중간~높음
- 확신도: 높음
- 작업량: 낮음
- 권장 작업: 데모 씬은 Build Settings에 넣지 않는다. 현재 Build Settings는 게임 씬 5개로 제한되어 있어 괜찮지만, 릴리즈 전에는 사용하지 않는 서드파티 데모 폴더를 `Assets` 밖의 보관 폴더로 옮기는 것을 권장한다. 특히 `Resources` 또는 직접 참조된 프리팹은 데모 씬이 아니어도 빌드에 포함될 수 있다.
- 검증 방법: 빌드 리포트의 included assets를 확인해 데모 씬/데모 에셋 의존성이 들어가지 않았는지 확인한다.

### 4. Renderer 수가 많은데 LODGroup이 없음

- 영역: LOD / 드로우콜 / GPU
- 근거: MeshRenderer 3,232개, SkinnedMeshRenderer 132개, LODGroup 0개
- 영향도: 중간
- 확신도: 중간
- 작업량: 중간
- 권장 작업: 바위, 나무, 절벽, 반복 장식물, 멀리 보이는 맵 오브젝트처럼 중거리/원거리에서 계속 보이는 오브젝트에 LODGroup을 추가한다. 작은 픽업, UI 라벨, 항상 카메라 가까이에 있는 플레이어/적 모델은 Profiler 근거가 나오기 전까지 우선순위를 낮게 둔다.
- 검증 방법: 게임플레이 카메라 경로에서 LOD 적용 전후의 triangles, batches, GPU frame time을 비교한다.

### 5. MeshCollider 수와 충돌 레이어 매트릭스 점검이 필요함

- 영역: 물리
- 근거: Collider 1,965개, MeshCollider 731개. Unity 물리 검증에서 모든 레이어가 서로 충돌 중이라고 보고됨
- 영향도: 중간
- 확신도: 레이어 매트릭스는 높음, MeshCollider 런타임 비용은 중간
- 작업량: 낮음~중간
- 권장 작업: `Player`, `Enemy`, `Terrain`, `Pickup`, `Trigger`, `Decoration`, `UI/Ignore` 같은 게임플레이 레이어를 정의하고 불필요한 충돌 조합을 끈다. 동적 오브젝트나 자주 접촉하는 오브젝트의 MeshCollider는 가능한 primitive collider 또는 compound collider로 교체한다.
- 검증 방법: Physics Profiler에서 FixedUpdate 비용, contact count, collision pair 수를 확인한다.

### 6. Transparent Multi-pass VFX 셰이더는 모바일에서 비용이 클 수 있음

- 영역: 셰이더/GPU / 모바일
- 근거: `Assets/Vefects/Free Blood VFX/...` 셰이더에서 12 pass, keyword pragma 60~84개 패턴 확인. 다수는 transparent/multi-pass임
- 영향도: 중간
- 확신도: 중간
- 작업량: 중간
- 권장 작업: 첫 모바일 친화 버전에서는 짧게 재생되는 one-shot VFX, 낮은 overdraw VFX를 우선 사용한다. 사용하지 않는 VFX shader variant를 strip하고, 전체 화면에 가까운 투명 이펙트를 겹쳐 쓰지 않는다. Blood/Fog 계열 이펙트는 Scene Overdraw 또는 Frame Debugger로 확인한다.
- 검증 방법: Frame Debugger에서 pass 수와 overdraw를 보고, 충돌/먹기 이펙트가 많은 상황에서 타깃 기기 GPU 시간을 측정한다.

### 7. 오디오 Import 설정을 정리할 필요가 있음

- 영역: 오디오 / 메모리
- 근거: `Assets/Sounds/Main Menu BGM.mp3`는 4.0 MB이며 decompress-on-load 후보로 플래그됨. `Game Over SFX.wav`도 1.0 MB로 동일한 후보임. 여러 짧은 SFX가 loadType 0, compressionFormat 1을 사용 중
- 영향도: 중간
- 확신도: 높음
- 작업량: 낮음
- 권장 작업: BGM과 긴 클립은 Streaming 또는 Compressed In Memory를 우선 검토한다. 중간 길이 SFX는 ADPCM 또는 Compressed In Memory를 검토하고, Decompress On Load는 아주 짧고 자주 재생되는 SFX에만 제한한다.
- 검증 방법: Memory Profiler에서 Audio 메모리를 확인하고, 타이틀/게임 씬 진입 시 로딩 스파이크를 비교한다.

### 8. 런타임 스크립트는 바로 대수술하지 말고 Profiler로 우선순위를 잡아야 함

- 영역: CPU / Scripts
- 근거: 정적 스캔 합계 기준 Update 계열 메서드 30개, GetComponent 호출 지점 172개, Instantiate/Destroy 호출 지점 51개, Resources.Load 호출 지점 13개, LINQ 사용 지점 12개. 런타임 게임플레이 예시는 `PlayerDinoController`, `EnemyWanderMovement`, `EnemySpawner`, 여러 UI Update 스크립트
- 영향도: 중간
- 확신도: 중간
- 작업량: 중간
- 권장 작업: 리팩터링 전에 `GameScene`에서 목표 적 수에 가까운 상태로 CPU Profiler를 먼저 캡처한다. 우선 확인할 지점은 `EnemyWanderMovement`의 적별 `Update`/`FixedUpdate`, `EnemySpawner`의 spawn/despawn 경로, 매 프레임 애니메이션하는 UI 스크립트다. 이미 있는 pooling service 방향은 좋으므로, Profiler에서 스파이크가 보이면 VFX, 드랍, 적 재사용으로 확장한다.
- 검증 방법: Profiler CPU Timeline, GC Allocation 컬럼, 필요한 경우 Deep Profile 스팟 체크.

## 영역별 메모

### URP / 모바일

현재 활성 URP PC 프로파일은 HDR Off, MSAA x1, Additional Lights 0, Soft Shadows Off라 비교적 보수적인 편이다. 모바일 품질은 `Mobile` quality level을 별도로 확인해야 한다. 우선 권장값은 기기 성능에 따라 Render Scale 0.8~1.0, PC보다 낮은 Shadow Distance, 가능한 경우 Cascade 1개, 꼭 필요한 Renderer Feature만 유지하는 방향이다.

### 조명과 그림자

스캔된 에셋/씬 전체에서 Light 72개가 확인됐다. `Dino Grow 3D`는 야외 로우폴리 맵 중심으로 보이므로, 정적 장식물은 baked/mixed lighting을 우선 검토하고 realtime shadow caster 수를 작게 유지하는 편이 좋다. 작은 풀, 꽃, 픽업, 원거리 장식물의 그림자는 꺼도 체감 품질 손실이 작을 가능성이 높다.

### UI

Canvas 35개는 그 자체로 문제라고 볼 수는 없지만, HUD, EXP 플로팅 텍스트, 게임오버 UI, 로딩 오버레이는 갱신 빈도별로 분리하는 것이 좋다. 정적인 HUD 패널이 플로팅 텍스트나 경고 이펙트 때문에 같이 rebuild되지 않도록 해야 한다. 장식용 Image/Text는 Raycast Target을 끈다.

### Terrain

Terrain 7개와 TerrainData 14개가 확인됐다. 모바일 기준으로는 terrain detail density, tree/grass rendering, terrain pixel error, basemap distance, terrain collider 비용을 맵별로 확인해야 한다. 로우폴리 맵이 대부분 mesh prop으로 구성되어 있다면 Terrain은 단순하게 유지하고 detail layer를 과하게 쓰지 않는 편이 좋다.

### 2D

SpriteRenderer 127개와 Sprite Atlas 1개가 확인됐지만 Tilemap, 2D Physics, 2D Light, SpriteSkin은 발견되지 않았다. 따라서 2D 최적화는 별도 게임플레이 최적화보다는 UI/Sprite 정리 관점으로 보면 된다.

### 네트워크 / Addressables

네트워크 프레임워크와 Addressables 설정은 발견되지 않았다. 프로토타입 단계에서는 Addressables가 필수는 아니지만, 스테이지 맵, 공룡 팩, 선택형 VFX/Audio 콘텐츠가 커지면 도입 가치가 생긴다.

## 빠른 개선 목록

- 사용하지 않는 데모/예제 폴더를 릴리즈 빌드 전 `Assets` 밖으로 이동하거나 보관한다.
- `Resources` 아래 에셋을 전수 확인하고 의도적으로 런타임 로드하는 것만 남긴다.
- 가장 큰 지형/스카이박스/VFX 텍스처부터 플랫폼별 override를 적용한다.
- BGM import 설정을 Decompress On Load가 아닌 Streaming 또는 Compressed In Memory 쪽으로 조정한다.
- Physics Layer Collision Matrix에서 불필요한 충돌 조합을 끈다.
- 작은 장식물과 파티클의 그림자를 끈다.
- 멀리서도 반복적으로 보이는 맵 오브젝트에만 우선 LODGroup을 추가한다.

## 추가 조사 필요 항목

- `GameScene`, `map4`, `map7`, `map10`을 Play Mode에서 목표 적 수에 가깝게 실행한다.
- CPU frame time, GC alloc, Physics, Animation, UI rebuild, Rendering 관련 Profiler marker를 캡처한다.
- Frame Debugger로 SetPass calls, transparent passes, shadow caster count, SRP Batcher 호환성을 확인한다.
- 게임플레이 진입 후와 맵 전환 후 Memory Profiler snapshot을 찍는다.
- 모바일 타깃이 있다면 15~30분 장시간 테스트로 발열, 배터리, sustained frame pacing을 확인한다.

## 변경 전 기준으로 남겨둘 측정값

Import 설정이나 씬 구조를 바꾸기 전에 아래 값을 기준선으로 남기는 것을 권장한다.

- FPS, CPU frame time, GPU frame time
- Batches, SetPass calls, triangles, vertices, shadow casters
- Texture memory, render texture memory, total reserved/used memory
- 프레임당 GC allocations
- Physics fixed-step time, contact count
- UI layout rebuild count
- 씬 로딩 시간, 최종 빌드 크기
