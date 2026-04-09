using NUnit.Framework;

namespace HPAICOmpanionTester.Features;

// Controls test execution order across features.
// NUnit runs fixtures in ascending Order value.

[Order(1)]
public partial class ApplicationLaunchFeature;

[Order(2)]
public partial class LeftNavigationFeature;

[Order(3)]
public partial class ChatInteractionFeature;

[Order(4)]
public partial class EditSentMessageFeature;

[Order(5)]
public partial class PerformAgentDeviceControlFeature;

[Order(6)]
public partial class PerformAgentCommandProbeFeature;
