This is a prototype of lightweight DI Container. It uses reflection in runtime and works via attributes. 
Main purpose of this project it's an attempt to create a handy and at the same time swift system.

For example to create a controller you only have to add attribute [Controller] right before your class.
If your controller needs a dependency such as MonoBehaviour or an interface of other controller you have to create a private field with [Inject] attribute.

Performance tests with 10k(!) controllers and 30 [Inject] fields each have shown pretty good results.
Editor execution time was approximately 550 ms and Android APK(NOX) was around 1700 ms.
Of course none developer can create 10k controllers and support them. 
More close to real life scenarious (30 controllers) took 10 ms Editor and around 250 ms APK.

Every controller shown in the project are only for example and showcase. 
Core of the system is written in EntryPoint.cs https://github.com/onethousandvoices/ZenjectStyleEntryPoint/blob/main/Assets/Scripts/Core/EntryPoint.cs
