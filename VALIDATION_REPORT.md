# CommonClass 전체 검증 보고서

검증일: 2026-09-01  
대상: `D:\Programs\CommonClass`  
기준: .NET Framework 4.8 / C# 7.3 호환성, 자원 정리, 예외 처리, 동시성, 로컬 실행 가능 여부

## 종합 결과

- Debug 전체 빌드: 성공, 오류 0개
- Release 전체 빌드: 성공, 오류 0개
- CommonClass WinForms: 초기화 후 3초간 생존 확인
- JobHandler 자동 시험: 성공
- LogHandler 200건 기록 및 Dispose 시 Queue flush: 성공
- SerialHandler 장비 없는 실패/Dispose 경로: 성공
- SocketClient loopback 송수신: 연결 및 송신 성공, TCP 분할 수신 위험 재현
- DBHandler 실제 연결: 접속정보가 없어 미수행
- SerialHandler 실제 송수신: COM 장비가 없어 미수행

현재 빌드 실패를 일으키는 문제는 없습니다. 다만 실운영 전에 우선 검토할 문제 2건과 운영 안정성 개선사항이 있습니다.

## 2026-09-01 개선 적용

- LogHandler Thread를 background로 전환하고 기본 5초 종료 제한 및 `Stop(timeout)` 결과를 추가
- Log Queue 기본 상한 10,000건과 `MaxQueueSize`, `DroppedLogCount` 추가
- AsyncSocket의 존재하지 않는 `AllRules.ruleset` 설정 제거
- MSSQL/Oracle 정적 연결 문자열 필드를 private으로 변경
- 적용 후 Debug/Release 빌드: 경고 0개, 오류 0개
- Log 200건 flush 및 Queue 포화/drop/제한 종료 회귀시험 통과

## 발견 사항

### [높음] SocketClient의 `ReceiveData(length)`가 지정 길이를 보장하지 않음

위치: `SocketClient\clsSocketClient.cs` 232~289행

`ReceiveData(6)`은 내부에서 `NetworkStream.Read`를 한 번만 호출합니다. TCP는 메시지 경계를 보장하지 않으므로 상대가 6바이트를 보내더라도 2바이트와 4바이트처럼 나뉘어 도착할 수 있습니다.

로컬 시험에서 서버가 `AB`와 `CDEF`를 150ms 간격으로 전송했을 때 `ReceiveData(6)`은 첫 2바이트만 반환했습니다. 호출자가 6바이트 전문 전체가 도착했다고 가정하면 불완전한 메시지를 정상 데이터로 처리할 수 있습니다.

권장 방향:

- 현재 API를 "최대 length만큼 한 번 읽기"로 명시하고 이름/문서를 명확히 하거나
- 지정 길이까지 반복 수신하는 `ReceiveExact(int length)`를 별도로 제공
- 구분자 또는 길이 헤더 기반 프로토콜이라면 전용 framing 계층 추가

### [높음] MSSQL 정적 연결/트랜잭션 API가 동시 호출에 안전하지 않음

위치: `DBHandler\MSSql.cs` 28~36행, 955~1058행

`MSSQLDbAccess`는 연결 문자열, `SqlConnection`, `SqlTransaction`을 정적 필드로 공유합니다. `ConnectionString`, `BeginTransaction`, `ExecuteNonQuery`, `Commit`, `Rollback` 사이에 동기화가 없습니다.

여러 Thread 또는 서로 다른 Job이 동시에 이 API를 사용하면 다음 문제가 가능합니다.

- 한 작업이 다른 작업의 연결 문자열을 덮어씀
- 두 작업이 동시에 Transaction을 시작하면서 Connection/Transaction 참조가 교체됨
- 한 작업의 Commit/Rollback/Cleanup이 다른 작업의 Transaction을 종료함

단일 프로세스에서 DB를 하나만 순차 사용한다면 드러나지 않지만, JobHandler로 여러 업무를 병렬 실행할 경우 위험도가 커집니다. 트랜잭션 작업은 우선 인스턴스 기반 `MSSQLDbAgent`를 Job별로 분리하여 사용하는 편이 안전합니다.

### [해결] LogHandler 종료가 무기한 대기할 수 있고 Log Thread가 foreground임

위치: `LogHandler\Log.cs` 271~318행

Log Thread는 `IsBackground`를 설정하지 않아 foreground thread로 실행됩니다. 또한 `Dispose`는 timeout 없는 `thread.Join()`을 호출합니다. 정상 파일 시스템에서는 Queue를 모두 기록하고 잘 종료되는 것을 확인했지만, 네트워크 드라이브·잠긴 파일·장시간 I/O 정체가 발생하면 프로그램 종료가 무기한 지연될 수 있습니다. 사용자가 Dispose를 누락하면 foreground thread 때문에 프로세스가 계속 살아 있을 수도 있습니다.

권장 방향은 background thread 지정, 종료 timeout 정책, flush 성공 여부 반환 또는 상태 노출입니다.

### [해결] Log Queue에 용량 제한이 없음

위치: `LogHandler\Log.cs` 35행, 496~509행

Queue의 초기 capacity는 100이지만 최대 크기 제한은 없습니다. 로그 생산 속도가 디스크 기록보다 지속적으로 빠르거나 기록 경로가 느려지면 메모리 사용량이 계속 증가할 수 있습니다.

운영 정책에 따라 최대 Queue 크기, 초과 시 drop/block 정책, drop count 상태를 제공하는 것이 좋습니다.

### [해결] AsyncSocket의 코드 분석 규칙 파일 누락

위치: `AsyncSocket\AsyncSocket.csproj` 52행, 62행

Debug/Release 빌드에서 `AllRules.ruleset`을 찾을 수 없다는 MSB3884 경고가 각각 1개 발생합니다. 컴파일 결과에는 영향이 없지만 정적 분석이 실제로 수행되지 않습니다. 규칙 파일을 저장소에 추가하거나 존재하는 ruleset 경로로 변경하거나 해당 설정을 제거해야 합니다.

### [해결] DB 정적 연결 문자열이 public mutable 필드임

위치: `DBHandler\MSSql.cs` 36행, `DBHandler\Oracle.cs` 35행

사용자명과 비밀번호가 들어간 연결 문자열이 `public static string`으로 노출되어 임의 변경이 가능하고 프로세스 전역에서 공유됩니다. 최소한 private 필드와 설정 메서드/읽기 전용 상태로 제한하는 것이 안전합니다.

## 정상 확인 사항

### JobHandler

- 작업 완료 후 Interval 대기 순서 확인
- 동일 Job 중첩 실행 없음
- 업무 함수 예외 후 다음 주기 계속 실행
- RunCount/ErrorCount/LastException 갱신 확인
- 개별 및 전체 Stop의 Join 종료 확인
- 잘못된 Interval과 중복 이름 검증 확인
- `Thread.Abort` 미사용 확인
- DB/Log/Serial/Socket 의존성 없음

### LogHandler

- Queue 접근 잠금 적용
- 로그 200건 enqueue 후 Dispose 시 200건 전부 기록 확인
- 파일명 invalid character 치환 확인
- 개별 파일 쓰기 실패가 전체 Queue 처리를 중단하지 않도록 격리
- 오래된 로그 삭제 중 개별 파일 오류 격리

### SerialHandler

- 포트명, baud, data bits, stop bits, parity, flow 검증 존재
- 잘못된 COM 포트 Open 시 `false`와 `ErrMsg` 반환 확인
- STX/ETX 분할 프레임 누적 처리 구현 확인
- 이벤트 구독자별 예외 격리 확인
- Close/Dispose 시 이벤트 해제와 수신 버퍼 정리 확인

### AsyncSocket

- 부분 Send 완료까지 재호출하는 처리 존재
- Send Queue 직렬화 적용
- Receive 중복 시작 방지
- 연결 generation으로 오래된 callback 격리
- Accept loop가 구독자 예외로 종료되지 않도록 격리
- Close/Dispose 중 Socket 정리 확인

### DBHandler

- DataTable/DataSet 계열에서 Connection/Command/Adapter using 처리 확인
- Reader 반환 API에서 `CommandBehavior.CloseConnection` 적용 확인
- Agent 계열 Disconnect/Dispose 시 Reader/Command/Transaction/Connection 정리 확인
- 명령 timeout 적용 확인
- Parameter name/value 개수 검증 존재

## 환경이 필요한 추가 검증

- MSSQL: 실제 연결, Stored Procedure, Transaction commit/rollback, timeout, 동시 Job 부하
- Oracle: 실제 연결, RefCursor, Transaction, 연결 장애 복구
- Serial: 실제 장비에서 분할 STX/ETX, 다중 프레임, 케이블 분리/재연결
- AsyncSocket: 대용량 송수신, 다중 client, 반복 연결/종료, 네트워크 단절
- LogHandler: 느린 네트워크 경로, 디스크 부족, 파일 잠금, 대량 Queue 부하

## 권장 처리 순서

1. SocketClient에 exact-length 수신 API 또는 명확한 framing 정책 추가
2. MSSQL 정적 Transaction API의 동시성 정책 확정 및 인스턴스 기반 사용 유도
3. LogHandler 종료 timeout과 Queue 상한 정책 추가
4. AsyncSocket ruleset 경고 정리
5. 실제 DB/COM 환경 통합 시험
