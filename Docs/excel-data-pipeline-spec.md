# 엑셀 데이터 파이프라인 사양

이 문서는 Dino Grow 3D의 엑셀 양식 생성과 데이터 변환 기능의 사양을 정의한다.  
엑셀 데이터 로딩은 DI 기반으로 설계하며, 구현체는 `IDataService` 인터페이스 뒤에 둔다.

## 목표

- NPOI를 사용해 `.xlsx` 파일을 읽고 쓴다.
- 빈 엑셀 양식을 Unity 메뉴에서 생성한다.
- 엑셀 데이터를 `ScriptableObject`로 변환한다.
- 데이터 로직은 직접 싱글톤 호출이 아니라 DI로 참조할 수 있어야 한다.
- 에디터 자동화는 `[MenuItem]`으로 제공한다.

## NPOI 설치

NPOI DLL은 아래 저장소를 기준으로 설치했다.

```text
https://github.com/sarmalev2/NPOI-for-Unity-6
```

설치 위치:

```text
Assets/Plugins/NPOI
```

해당 저장소 README 기준으로 Unity 6에서는 DLL 파일을 `Assets/Plugins` 아래에 넣어 Unity가 인식하게 한다.

## DI 구조

인터페이스:

```text
Assets/Scripts/Infrastructure/Data/IDataService.cs
```

구현체:

```text
Assets/Scripts/Infrastructure/Data/ExcelDataService.cs
```

`GameLifetimeScope`에서 등록한다.

```text
IDataService → ExcelDataService
```

## 현재 DinoTable 양식

시트 이름:

```text
DinoTable
```

헤더:

| 필드 | 설명 |
|---|---|
| id | 공룡 고유 ID |
| displayName | 표시 이름 |
| level | 공룡 레벨 |
| exp | 먹었을 때 획득 EXP |
| speed | 이동 속도 |
| size | 크기 |
| aiType | AI 타입 |
| colorType | 색상/상태 타입 |
| prefab | 프리팹 이름 또는 경로 |

## ScriptableObject 출력

출력 타입:

```text
DinoDatabase
```

파일:

```text
Assets/Scripts/Core/Data/DinoDatabase.cs
```

레코드 타입:

```text
DinoDataRecord
```

파일:

```text
Assets/Scripts/Core/Data/DinoDataRecord.cs
```

## Unity 메뉴

엑셀 양식 생성:

```text
Tools > Dino Game > Data > Create Dino Excel Template
```

엑셀 데이터를 ScriptableObject로 변환:

```text
Tools > Dino Game > Data > Convert Dino Excel To ScriptableObject
```

## 작업 규칙

- 데이터 서비스는 `IDataService`를 통해 참조한다.
- `ExcelDataService`를 직접 전역 싱글톤으로 만들지 않는다.
- MenuItem은 에디터 진입점일 뿐이고 실제 엑셀 로직은 `ExcelDataService`에 둔다.
- 런타임에서 필요한 데이터는 가급적 변환된 `ScriptableObject`를 사용한다.
- 에디터 전용 `AssetDatabase` 로직은 `Assets/Editor` 아래에만 둔다.
