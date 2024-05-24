This is a prototype of lightweight DI Container. It uses reflection and works via attributes. 
Main purpose of this project it's an attempt to create a handy system.

For example to create a controller you only have to add attribute [Controller] right before your class.
If your controller needs a dependency such as MonoBehaviour or an interface of other controller you have to create a private field with [Inject] attribute.

Every controller shown in the project are only for example and showcase. 
Core of the system is written in EntryPoint.cs https://github.com/onethousandvoices/ZenjectStyleEntryPoint/blob/main/Assets/Scripts/Core/EntryPoint.cs
