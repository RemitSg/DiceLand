# 游戏多语言

主菜单的「设置 → 游玩与反馈 → 语言」提供简体中文和 English。保存后应用，并写入 main_menu_settings.json。旧设置默认简体中文。英文覆盖主菜单、能力树和制作人员、地图探索与各面板、全部教学战脚本、地图事件与副本、战斗详情、战斗结算和整局结算。缺少译文的内容显示原文。

## 译表和打包

团结 1.8.4 使用 Localization 1.5.3。通过 **Window > Asset Management > Localization Tables** 编辑 DiceLand 表集合，源资产位于 Assets/Localization/Tables。静态标签用稳定语义键调用 GameText.Get。动态整句用 GameText.Format 和具名参数，两列保留同名参数。译文可调整语序。金币、行动点和状态可用 [[DL_Money|Gold]] 这样的标记保留颜色与图标。

资产与教学脚本的原文保留在现有配置。中文显示读取当前原文，中文表列供翻译和校对。英文在显示时按固定键取值。敌人、技能与状态的同编号变体可显式填写 localizationKey。新增技能时为技能资产指定独立的 localizationKey，并补齐名称和描述。更新文案后同步译表的原文与英文列。

事件使用 eventId、optionId 和 resultId 定位标题、叙事、预览与结果。地图副本使用 dungeonId 和格子索引。教程使用 stepId 或 reactionId，覆盖标题、正文和拦截操作时的提示。编辑事件流程时应保留既有标识，并检查后续事件和随机分支。

译表以本地 Addressables 组随游戏构建。导入改动后，构建对应平台的 Addressables 内容并验证启动、往返切换、读档、事件分支与结算。运行回归包含双语条目和参数检查、全部事件与技能的译文覆盖、英文技能详情生成，以及正式场景里的旧档恢复。

## 存档与机制标识

角色用 characterId 和旧档别名恢复；新手木剑、史莱姆粘液和零编号绿史莱姆敌群也有固定存档 ID 与旧别名。重新保存写入固定资产 ID，保留原队员实例 memberId。地图存档目录会沿已分配事件查找后续事件，以恢复分支引用。

蛛孕、噬亲、融入暗影和赌博继续使用 BattleSkillMechanic 触发。教学战指定角色授技使用角色 ID。译表新增的文本键只用于显示。

部分长篇支线和风味文案使用本地翻译辅助起稿。主要界面、教程、状态、战斗和遗物机制已经单独校对。术语统一使用 Speed、Speed Slot、Power、Energy、Clash、One-Sided Attack 和 AP。[本地翻译模型说明](https://huggingface.co/Helsinki-NLP/opus-mt-zh-en)
