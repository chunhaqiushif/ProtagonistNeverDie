# Protagonist Never Die（插件版）

《潜渊症 Barotrauma》模组「主人公不会死 / 异星遗物插槽耳机」的**官方插件系统（IBarotraumaPlugin）**实现。
早期版本是 LuaCs 内存内 C# 模组（`CSharp/Shared/ProtagonistNeverDie.cs`），本次迁移把代码与内容按插件规则重新组织：
不再依赖 Lua For Barotrauma，直接由游戏原生插件加载器载入编译好的 .dll。
