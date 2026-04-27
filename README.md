# HMS Unity SDK

## HMS Init Pipeline

The HMS SDK exposes a dedicated initialization pipeline that runs only after
the HMS bootstrap completes successfully.

Use HMSInit to mark project methods that must run when HMS services are
available.

### Why Use HMSInit

- Guarantees HMS bootstrap has completed before your method runs.
- Avoids startup races where services are requested before locator setup.
- Provides deterministic execution by runtime stage and order.
- Logs explicit diagnostics when signatures are invalid or execution fails.

### Method Rules

HMSInit methods must follow these requirements:

- Static method.
- No parameters.
- Return type must be void.
- Public or private are both supported.

If any rule is violated, HMS logs an explicit error and skips the method.

### Runtime Stages

The pipeline currently supports these stages:

- RuntimeInitializeLoadType.BeforeSceneLoad
- RuntimeInitializeLoadType.AfterSceneLoad

Default stage is AfterSceneLoad.

### Ordering

Use the optional order value to control priority.

- Lower values run first.
- Methods with the same order are sorted deterministically by method key.

### Basic Usage

```csharp
using HMSUnitySDK;
using UnityEngine;

namespace MyGame.Initialization
{
	public static class MyHMSInitHooks
	{
		[HMSInit]
		private static void ConfigureAfterSceneLoad()
		{
			if (!HMSLocator.TryGet(out HMSAuth authService, out string reason))
			{
				Debug.LogWarning(
					$"Auth service unavailable during HMSInit. Reason: {reason}"
				);
				return;
			}

			Debug.Log("HMSInit executed with HMS locator ready.");
		}
	}
}
```

### Advanced Usage With Stage And Order

```csharp
using HMSUnitySDK;
using UnityEngine;

namespace MyGame.Initialization
{
	public static class OrderedHMSInitHooks
	{
		[HMSInit(RuntimeInitializeLoadType.BeforeSceneLoad, order: -100)]
		private static void PrepareEarlyState()
		{
			Debug.Log("Early HMSInit step completed.");
		}

		[HMSInit(RuntimeInitializeLoadType.AfterSceneLoad, order: 10)]
		private static void ConfigureLateBindings()
		{
			if (!HMSLocator.TryGet(
					out HMSLauncherInteropsService interopsService,
					out string reason
				))
			{
				Debug.LogWarning($"Launcher interops unavailable. Reason: {reason}");
				return;
			}

			Debug.Log("Late HMSInit step completed.");
		}
	}
}
```

### What Happens On Bootstrap Failure

If HMS bootstrap fails, the pipeline does not run HMSInit methods and logs a
clear warning indicating that bootstrap was not completed.

This behavior is intentional to keep startup fail-safe and prevent cascading
null reference errors.

### Best Practices For Consumer Projects

- Keep HMSInit methods focused on initialization wiring.
- Resolve services through HMSLocator.TryGet for explicit fallback paths.
- Use order only when dependency between methods is real.
- Prefer idempotent setup logic in case game state is reloaded.
- Keep heavy asynchronous workflows outside direct startup hooks when possible.
