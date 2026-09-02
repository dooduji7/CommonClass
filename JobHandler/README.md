# 범용 반복 작업 실행 모듈 (1차)

## 적용 목적

업무 함수와 반복 실행 제어를 분리합니다. 사용자는 한 번 수행하고 반환하는 함수만 등록하며,
`JobManager`가 background thread 생성, 반복, 대기, 상태, 예외 격리, 종료 확인을 담당합니다.
DB, 로그, Serial, Socket 모듈에는 의존하지 않으므로 필요한 기능은 각 업무 함수 안에서 선택해 사용합니다.

대상은 .NET Framework 4.8 / C# 7.3입니다. 프로젝트가 명시적 Compile 항목을 쓰는 구형 형식이면
이 폴더의 `.cs` 파일 5개를 CommonClass 프로젝트에 포함해야 합니다.

## 사용 예제

```csharp
using CommonClass.Worker;
using System;

class Program
{
    static void Main()
    {
        using (JobManager manager = new JobManager())
        {
            manager.JobStateChanged += (sender, e) =>
                Console.WriteLine("{0}: {1} -> {2}", e.JobName, e.PreviousState, e.State);

            manager.JobError += (sender, e) =>
                Console.WriteLine("{0}: {1}", e.JobName, e.Exception.Message);

            manager.Register("DATA_CHECK", CheckData, 1000);
            manager.Register("HEARTBEAT", SendHeartbeat, TimeSpan.FromSeconds(5));
            manager.StartAll();

            JobStatus status = manager.GetStatus("DATA_CHECK");
            Console.WriteLine("{0} / {1} / {2}", status.Name, status.State, status.RunCount);

            Console.ReadLine();

            // 각 작업에 최대 5초씩 기다립니다. false면 사용자 함수가 아직 반환하지 않은 상태입니다.
            bool stopped = manager.StopAll(5000);
        }
    }

    private static void CheckData()
    {
        // 한 번의 업무만 수행하고 반드시 반환합니다.
    }

    private static void SendHeartbeat()
    {
        // 필요한 경우 기존 DB/Log/Serial/Socket 기능을 여기서 사용합니다.
    }
}
```

개별 제어는 `Start(name)`, `Stop(name, timeout)`, `Restart(name, timeout)`을 사용합니다.
`Stop`은 등록을 유지하고, `Remove(name, timeout)`은 정상 정지된 작업만 등록에서 제거합니다.
존재하지 않는 이름은 `KeyNotFoundException`, 중복 이름은 `InvalidOperationException`으로 알려줍니다.

## 실행 및 종료 정책

실행 순서는 `업무 함수 1회 완료 -> Interval 대기 -> 다음 실행`입니다. 작업별 단일 thread가
순차 실행하므로 같은 작업이 중첩되지 않습니다. 업무 함수에서 발생한 예외는 `JobError` 이벤트와
상태 정보에 기록한 뒤 다음 주기를 계속 수행합니다.

`Stop`은 stop signal을 설정하고 `Thread.Join(timeout)`으로 종료를 확인합니다. 대기 중인 작업은 즉시
깨어나 종료하지만 이미 실행 중인 사용자 함수에는 강제 개입하지 않습니다. timeout 안에 반환하지 않으면
`false`를 반환하고 상태는 `Stopping`으로 남으며, 함수가 나중에 반환하면 `Stopped`로 전환됩니다.
`Thread.Abort`는 사용하지 않습니다.

등록 함수 안에 `while (true)`, 자체 반복용 `Thread.Sleep`, 별도 Timer를 만들지 마십시오. WinForms
컨트롤도 background thread에서 직접 수정하지 말고 UI thread로 전달해야 합니다.

## 상태 의미

- `State`: 작업 생명주기(`Stopped`, `Starting`, `Running`, `Stopping`)
- `IsExecuting`: 현재 업무 함수가 실제 실행 중인지 여부 (`Running`이어도 Interval 대기 중이면 `false`)
- `RunCount`: 업무 함수를 실행한 총 횟수(오류 실행 포함)
- `ErrorCount`: 예외 발생 횟수
- `LastStartTime`, `LastEndTime`, `LastExecutionTime`: 직전 실행 측정값
- `LastException`: 가장 최근 업무 함수 예외

`GetStatus`와 `GetAllStatus`는 내부 값이 이후 바뀌지 않는 snapshot을 반환하므로 Console/WinForms
표시에 바로 사용할 수 있습니다. 상태 및 오류 이벤트는 worker thread에서 호출되며, 이벤트 구독자의
예외가 실행 모듈을 중단시키지 않도록 격리됩니다.
