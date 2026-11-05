# 多语言第一阶段

主菜单的「设置」提供简体中文和 English。保存后切换并写入 `main_menu_settings.json`。旧设置默认简体中文。英文样板覆盖主菜单和设置、地图开场与首战教程、「泥潭」两个选项，以及教程首战的敌人档案、技能详情和结算。其他条目在缺少译文时显示原文。

## 译表和打包

团结 1.8.4 使用 Localization 1.5.3。通过 **Window > Asset Management > Localization Tables** 编辑 `DiceLand` 表集合，源资产位于 `Assets/Localization/`。新增文字用稳定语义键，静态标签调用 `GameText.Get`，动态整句通过 `GameText.Format` 传具名参数。技能和敌人可设置 `localizationKey`。事件用 `eventId` 和 `optionId` 查表，教程用 `stepId` 或 `reactionId` 查表。

译文中的 `[[DL_Money|Gold]]` 之类标记用图标 ID 绑定颜色和资源图标，竖线后的文案随语种填写。数量、触发条件和目标从玩法数据读取。译表以本地 Addressables 组随构建打包，运行时在译表可用后切换并刷新菜单。

## 存档与机制标识

角色有独立的 `characterId` 和旧档别名。新手木剑、史莱姆粘液、零编号绿史莱姆敌群也配置了固定存档 ID 和原来使用的名称别名。旧档读取时接受别名，后续保存写入固定资产 ID，并保留原存档中的队员实例 `memberId`。资产目录会沿已分配的地图事件查找后续事件，以便恢复相应引用。

蛛孕、噬亲、融入暗影和赌博用 `BattleSkillMechanic` 触发。教学战的指定角色技能授予使用角色 ID，旧配置的目标名按兼容别名查找。显示译文只在界面读取时生成。
