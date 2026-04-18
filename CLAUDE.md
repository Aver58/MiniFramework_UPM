# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

This is a lightweight Unity game framework, distributed as a Unity Package Manager (UPM) package. It provides core infrastructure for Unity game development with a modular architecture.

## Architecture

### Core Modules

**1. Singleton** (`MiniFramework/Singleton/`)
- `Singleton<T>`: Generic non-Mono singleton pattern
- `MonoSingleton<T>`: Generic MonoBehaviour singleton

**2. Event System** (`MiniFramework/EventManager/`)
- `EventManager`: Observer pattern event system using C# delegate dictionary
- Supports up to 4 generic type parameters for event payloads
- Uses `EventConstantId` to define event IDs as integers

**3. Resource Management** (`MiniFramework/Resource/`)
- `ResourceManager`: High-level resource manager with reference counting and caching
- `IResourceLoader`: Loader abstraction, supports two implementations:
  - `AssetBundleLoader`: Traditional AssetBundle loading
  - `AddressableLoader`: Unity Addressables loading
- `ResourceConfig`: ScriptableObject configuration for load mode selection
- `ResourceCache`: LRU-style cache for loaded assets

**4. Game World** (`MiniFramework/Base/`)
- `GameWorld`: Root game world container for feature modularity
- `IGameWorldFeature`: Feature interface for pluggable game world features
- `UpdateRegister`: Unity Update callback registration for non-Mono classes

**5. MVVM UI Framework** (`MiniFramework/UI/MVVM/`)
- `UIFramework`: Main entry point, static API for opening/closing UI
- `BaseViewModel`: ViewModel base class with property binding and lifecycle events
- `BaseView`: View base class connected to ViewModel
- `BaseModel`: Model base class with INotifyPropertyChanged
- `UILayer`: Enum for UI layer sorting (Background, Normal, Popup, Top)
- `UIStackManager`: UI navigation stack management
- `UIObjectPool`: Object pooling for frequently used UI elements

## Commands

This is a Unity UPM package - use Unity Editor to:
- Build/Compile: Unity automatically compiles C# files in the Editor
- Test: Create test scenes in a dependent project and run in Play Mode
- Clean: Use Unity's "Clear Library" menu option when needed

## Code Conventions

- C# for all code, follows standard Unity C# conventions
- All framework code lives under `MiniFramework/` root namespace
- Singleton pattern used for manager classes
- Interface-based design for extensible components (IResourceLoader, IGameWorldFeature)
- MVVM separates UI logic from presentation
