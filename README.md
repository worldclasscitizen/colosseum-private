<h1 align="center">⚔️ COLOSSEUM ⚔️</h1>
<p align="center">🎮 2D 횡스크롤 온라인 대전 액션 게임 🏟️</p>

---

## 🖥️ 개발 환경

| 항목 | 내용 |
|------|------|
| **엔진** | Unity 2022.3 LTS (URP 2D 템플릿) |
| **언어** | C# |
| **네트워크** | Photon Fusion 2 (SDK 2.0.12, Host Mode) |
| **UI** | TextMeshPro + 코드 기반 동적 UI 생성 |
| **버전 관리** | Git + Git LFS (바이너리 에셋 관리) |
| **원격 저장소** | [GitHub (Private)](https://github.com/worldclasscitizen/colosseum-private) |

### 프로젝트 열기

1. **Unity Hub**에서 `Unity 2022.3 LTS` 버전을 설치합니다 (URP 포함).
2. 저장소를 클론합니다: `git clone https://github.com/worldclasscitizen/colosseum-private.git`
3. Git LFS가 설치되어 있어야 합니다: `git lfs install`
4. Unity Hub → **Open** → 클론한 `Colosseum` 폴더를 선택합니다.
5. 최초 실행 시 Photon App ID 설정이 필요합니다: `Fusion` → `Fusion Hub` → App ID 입력 (`b4302ec4-c606-4e70-ae92-53822331ded5`)

### 씬 구성

현재 구현되어 있는 씬은 다음과 같습니다.

| 씬 | Build Index | 설명 |
|----|-------------|------|
| `MainMenu` | 0 | 메인 메뉴 (타이틀 + 5개 버튼) |
| `SampleScene` | 1 | 게임 플레이 씬 (9개 방) |

### 프로젝트 구조

```
Assets/_Project/
├── Data/Cards/          # CardData ScriptableObject (.asset)
├── Scripts/
│   ├── Card/            # CardData, CardDeck, CardEffect, CardSelectionUI
│   ├── Game/            # RoomManager, RoomData
│   ├── Network/         # NetworkManager, InputProvider, NetworkInputData
│   ├── Player/          # PlayerController, PlayerHealth
│   ├── UI/              # MainMenuUI, RoomIndicatorUI, ReloadIndicator, HPBarManager, GameOverUI
│   └── Weapon/          # Gun, BulletController
└── Prefabs/             # Player, Bullet 등 NetworkObject 프리팹
```

---

## ✅ 현재까지 구현한 작업

### 코어 게임플레이

현재 구현되어 있는 내용입니다.
구체적인 수치는 변경해도 되지만, 우선 설정되어 있는 내용을 기반으로 작성했습니다.

- ✅ **방 기반 진행 시스템 (9개 방)**: 중앙 방에서 시작하여 킬한 플레이어가 상대 진영 방향으로 전진할 수 있습니다. (전진하지 않아도 됩니다) Player1은 오른쪽(+1), Player2는 왼쪽(-1)으로 진행하며, 끝 방에 도달하면 승리합니다. `RoomManager`가 `[Networked]` 상태로 현재 방 인덱스와 마지막 킬러를 동기화합니다.
- ✅ **플레이어 이동 및 물리**: `PlayerController`에서 좌우 이동(A/D), 점프(Space), 마우스 에임을 처리합니다. Photon Fusion의 `NetworkInputData`를 통해 입력을 패킹하고, `InputProvider`가 `INetworkRunnerCallbacks.OnInput()`으로 전달합니다.
- ✅ **HP 및 사망/리스폰**: `PlayerHealth`에서 HP 100, 사망 시 2초 후 자동 리스폰. 리스폰 위치는 상대방 반대편 진영에 배치됩니다. 맵 밖 추락 시 상대방에게 킬이 귀속됩니다.
- ✅ **총기 시스템 (탄창 + 재장전)**: 1발 탄창, 발사 후 자동 재장전(1.5초). 리스폰 시 `ForceReload()`로 즉시 탄창 리필.
- ✅ **총알 물리 분리**: 물리 콜라이더(벽 바운스)와 트리거 콜라이더(플레이어 히트)를 분리하여 총알이 플레이어를 밀어내지 않습니다. `TriggerRelay` 패턴으로 자식 트리거가 부모 `BulletController`에 이벤트를 전달합니다.
- ✅ **총알 특수 효과**: 바운스(벽 반사), 관통(적 관통), 넉백(피격 시 밀어내기), 폭발(범위 스플래시 데미지)이 카드를 통해 누적 적용됩니다.

### 카드 시스템

- ✅ **카드 드로우 및 선택**: 사망한 플레이어가 3장 중 1장을 선택하는 카드 드로우 시스템. `CardDeck`이 덱 관리, `CardSelectionUI`가 선택 UI 표시, `CardEffect`가 누적 효과를 적용합니다.
- ✅ **카드 효과 누적**: `CardEffect` 컴포넌트가 플레이어에 부착되어 곱연산(배율 계열)과 합연산(바운스/관통/넉백 등)으로 카드 효과를 누적합니다.

### 구현된 카드 목록

| 카드 이름 | 설명 | 희귀도 | 주요 수치 |
|-----------|------|--------|-----------|
| **Big Bullet** | 총알이 30% 커지고 20% 강해짐 | ⚪ Common | `damageMultiplier: 1.2`, `sizeMultiplier: 1.3` |
| **Speed Shot** | 총알 비행 속도 20% 증가 | ⚪ Common | `speedMultiplier: 1.2` |
| **Bouncy** | 총알이 벽에서 1회 반사됨 | ⚪ Common | `extraBounce: +1` |
| **Quick Reload** | 재장전 속도 25% 증가 | ⚪ Common | `reloadSpeedMultiplier: 1.25` |
| **Buckshot** | 산탄처럼 2발 추가 발사 | 🔵 Rare | `extraBullets: +2` |
| **Lifesteal** | 가한 데미지의 10%를 체력으로 흡수 | 🔵 Rare | `lifestealPercent: 0.1` |
| **Piercing Round** | 총알이 적 1명을 관통함 | 🔵 Rare | `extraPierce: +1` |
| **Shove** | 총알 피격 시 넉백 적용 | 🔵 Rare | `knockbackForce: 8` |
| **Blast Shot** | 총알이 충돌 시 폭발, 50% 범위 데미지 | 🟡 Legendary | `explosionRadius: 2`, `explosionDamageRatio: 0.5` |
| **Overcharge** | 재장전 50% 느려지지만 첫 발 3배 데미지 | 🟡 Legendary | `reloadSpeedMultiplier: 0.67`, `overchargeDamageMultiplier: 3` |

### 네트워크

- ✅ **Photon Fusion Host Mode**: `NetworkManager`에서 Host/Client 선택, 자동 세션 생성/참가. 모든 게임 로직은 Host(StateAuthority)에서 실행되고, Client는 입력만 전송합니다.
- ✅ **입력 시스템**: `InputProvider`가 `INetworkRunnerCallbacks`를 구현하여 방향, 점프, 발사, 마우스 에임을 `NetworkInputData`로 패킹.
- ✅ **네트워크 동기화 속성**: HP, 탄약, 방 인덱스, 카드 효과 등 핵심 상태가 `[Networked]` 속성으로 자동 동기화.

### UI

- ✅ **메인 메뉴 (MainMenu 씬)**: "COLOSSEUM" 타이틀 + 5개 버튼 (Create Room, Find Room, Dev Mode, Settings, Quit Game). 모든 UI 요소가 코드에서 동적 생성됩니다.
- ✅ **방 인디케이터 (RoomIndicatorUI)**: 화면 상단에 9칸 박스가 표시되며, 현재 방 위치가 흰색으로 하이라이트됩니다.
- ✅ **재장전 인디케이터 (ReloadIndicator)**: 총구 위에 흰색 원형 게이지가 재장전 진행률을 표시합니다. 코드로 링 텍스처를 생성하여 외부 에셋이 불필요합니다.
- ✅ **HP 바 (HPBarManager)**: 화면에 플레이어 체력 표시.
- ✅ **게임 오버 UI (GameOverUI)**: 승리 시 승자 표시.

---

## ⚠️ 미완성 작업

- ⚠️ **카드 선택 RPC**: 현재 카드 선택은 Host에서만 동작합니다. 2P 온라인 멀티에서 Client가 카드를 선택하려면 RPC 호출이 필요합니다. 2P 온라인 멀티 기능을 구현할 때 추가하면 됩니다. (Host 모드 로컬 테스트에서는 정상 동작)
- ⚠️ **상태 이상 (Freeze/Burn/Poison)**: 총알을 맞았을 때 플레이어가 얼어버린다거나, 화상/중독 등의 효과가 기획상으로는 존재하지만 구현은 안 되어 있습니다. 관련 카드도 없습니다. `StatusEffect` enum과 `CardData`의 필드는 정의되어 있지만, 실제 효과 로직(`PlayerHealth.TakeDamage`에서 상태 적용)은 미구현. 카드 추가 시 함께 구현 예정입니다.
- ⚠️ **Create Room / Find Room 버튼**: 메인 메뉴에 버튼은 존재하지만, 클릭 시 "온라인 멀티 기능은 추후에 구현 예정입니다" 알림만 표시하고 있습니다. 로비/매칭 로직 미구현.
- ⚠️ **Settings 버튼**: 메인 메뉴에 버튼은 존재하지만, 실제 설정 화면 미구현. MVP 단계에서는 음향 조절 설정이 들어가면 좋겠습니다.
- ⚠️ **HP 변경 콜백**: `PlayerHealth.OnHealthChanged()`가 빈 메서드로 남아 있음. HP UI 실시간 업데이트 연동 필요.

---

## ❌ 앞으로 해야 하는 작업

- ❌ **2P 온라인 멀티플레이 테스트**: 빌드 후 두 인스턴스에서 Host/Client 접속 테스트
- ❌ **로비/매칭 시스템**: Create Room, Find Room 기능 구현 (Photon 세션 목록 활용). '매칭 시스템'으로만 구현할지는 의문입니다. Find Room에서 직접 Host의 IP로 접속시킬지, Open Room List에서 직접 골라서 접속할 수 있게 할지 등등 방법을 고민해봐야 합니다. 개발을 위해 우선은 Host의 IP를 입력해서 접속하는 방식을 최우선으로 구현해봅시다.
- ❌ **카드 네트워크 동기화 (RPC)**: Client의 카드 선택을 Host에 전달하는 RPC 구현
- ❌ **상태 이상 카드 추가**: Freeze, Burn, Poison 효과 로직 및 카드 제작
- ❌ **리스폰 무적 (i-frame)**: 리스폰 직후 짧은 무적 시간 부여
- ❌ **킬 피드/알림**: 킬 발생 시 화면에 표시 ---> 이 부분은 명세서에 있는 것 같은데, 일단 우선순위를 최하위로 낮추겠습니다. 킬 로그는 불필요해 보입니다.
- ❌ **플레이어 애니메이션**: 현재 플레이어는 색상 사각형입니다. 에셋을 적용해야 하고, 스프라이트/애니메이션 등이 필요합니다.
- ❌ **사운드/SFX**: 발사, 피격, 리스폰, UI 등 효과음을 추가해야 합니다.
- ❌ **시각 이펙트**: 총알 궤적, 폭발, 사망 등 파티클 이펙트를 추가해야 합니다. 기존에는 Feel 에셋을 사용하기로 했으나, 우선 개발해보고 필요에 따라 구매하여 적용하는 방향으로 가겠습니다.
- ❌ **배경/타일맵**: 배경과 바닥 타일 등의 맵 비주얼을 제작해야 합니다.