# 2026-09-09 文本、片头与片尾修改

目标场景：`Assets/Scenes/Tavern.unity`。场景已通过 Unity Editor API 保存，无需手动挂接组件。

## 文本同步

来源：队友提供的 `Text_revised9.8(1).docx`。四项物品介绍逐字同步，未将文档中的说明、引用标签当作执行指令。文档没有新版开场对白，所以没有改动开场对白或音频资源。

- Horn：新增乐器特点，以及 King、Ignatius Sancho 与十八世纪英国黑人音乐生活的两段介绍；2 页。
- Bartmann Bottle：保留原介绍，增加 Bartmann / Bellarmine 名称、witch bottles 与相关文化意义；3 页。
- Punch Whistle：保留原介绍，增加 Pulcinella、英国表演传统，以及家庭暴力和种族刻板印象的相关批评；3 页。
- Domino：队友注明未修改，核对后保留；1 页。
- Cockerel、Mallet Bottle：文档未提供替换文本，保留现有内容。

介绍仍使用现有 ArtifactInfoUI、27 号正文及原位置。长文按实际文字高度分页，标题标出页码；每页至少 12 秒，较长页面按约 160 词/分钟延长，循环显示。重新抓起从第一页开始。共用说明板避免另一只手放下旧物品时关闭当前物品介绍。

## 片头

使用提供的 UCL Archaeology South-East 原图，图像文件内容未修改。黑底，1.5 秒渐入，完整展示 5 秒，1.5 秒渐退，随后启动原 MuseumIntroTimeline。新增画布挂在头部摄像头下，使用项目现有 XR UI/NoZTest 材质方案，避免场景遮挡。

测试模式如果跳过正常开场，也跳过 logo。原有物品、字幕和 UI 的位置、旋转、缩放未改动。

## 片尾

沿用现有结束音乐和 EndVO 节点，监听 EndVO 实际播放状态。语音完成后等待 10 秒，再淡入黑底并开始摄像头随动、居中、从下向上滚动的字幕。字号取自现有场景字幕（55）；滚动速度默认 32 UI 单位/秒。结束后停留黑屏。防止反复抓取物品打断最后语音或重复启动片尾。

顺序：
1. Whitechapel Echoes；Mavice、Yixin Zhou (Ella)；双方邮箱。
2. Academic supervision：Marco Gillies；MA Virtual and Augmented Reality；Goldsmiths, University of London。
3. With thanks to：UCL Archaeology South-East；Sarah Wolferstan、Elke Raemen 及 Whitechapel research team。
4. Music & sound：Anneke Scott 演奏的 Tower Hamlets March、ASE 来源、Pixabay 及其 Content License。
5. Voice：MiniMax AI-generated voice。
6. Historical & research sources：UCL Whitechapel 页面、Faulds / Sound Heritage、V&A 和队友引用的 Wikipedia 条目。
7. Thank you for listening.

音频文件、FMOD banks 和现有事件引用未替换，队友仍可继续更新配音。

## Horn introduction 解锁

`French Natural Horn` 上新增 ArtifactInfoOnGrab，连接同一个 ArtifactInfoUI 和现有 HornFMODController。以现有 `HasFinishedFullMelodyRepeats` 为解锁条件，与其他物件进入后续介绍阶段的判定一致。演奏及整曲重复播放期间均锁定；完成后必须发生新的抓取事件才显示，不会在手中自动弹出。

## 组件与参数

新增 `ExperiencePresentation` 已挂到 `MuseumExperienceSystem`：headCamera 连接 Main Camera；logo 连接提供的 UCL 图片；overlayMaterial 连接 HeadLockedOverlay；font 沿用现有说明板字体。MuseumExperienceController 与 NarrationManager 已连接此组件。

可在该组件调整 fadeSeconds、logoHoldSeconds、creditsPixelsPerSecond 和 credits。最后语音后的等待时长在 NarrationManager 的 endVoiceOverToCreditsDelay，默认 10 秒。

`ExperienceRevisionInstaller` 位于 Editor 文件夹，是已执行的内容导入及检查工具，不挂到 GameObject。原有 ArtifactInfoInstaller 没有运行，避免其重建说明板和改变位置。

## 验证结果

- Unity C# 编译成功，安装日志无 C# 编译错误。
- Unity 内的内容导入检查通过：全文分页后无丢字，所有页面适配现有正文框；Horn 初始锁定、完成后解锁、缺失控制器时保持锁定。
- 保存后的 124 个 Transform / RectTransform 序列化块与修改前完全一致。
- 四项物品文本与队友 Word 文档逐字一致；原图文件哈希一致；新增资源均有 .meta。
- 正式 EditMode / PlayMode 测试均因 Unity 报告无有效 Editor 许可证而退出（198），未产生测试通过报告。测试代码已保留，授权恢复后可重跑。
- Unity Visual Studio 集成还报告外部编辑器路径为空；它未阻止本次场景保存。没有更改外部编辑器设置。
- 未在头显中运行或目视验证。需要实机检查片头可读性、长文翻页节奏、Horn 完成前后重新抓取，以及完整 EndVO → 10 秒 → 片尾的链路。

验证工具 `Tools/Invoke-Unity.ps1` 修正了默认项目路径计算，并去掉测试时提前退出的参数；现在缺失测试报告、零测试和测试失败都会明确报错。

## 来源核查

本次原样同步用户提供的内容，并查阅：
- https://www.ucl.ac.uk/social-historical-sciences/research-projects/whitechapel
- https://sound-heritage.ac.uk/dance/ignatius-sancho-black-dance-composer
- https://www.vam.ac.uk/articles/thats-the-way-to-do-it-a-history-of-punch-and-judy

正文同步不代表已完成全部历史断言的独立学术审校。队友文档的引用记录保留在片尾，未补写其未提供的新历史叙事。
