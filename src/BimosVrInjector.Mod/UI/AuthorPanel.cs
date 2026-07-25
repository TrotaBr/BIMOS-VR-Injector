using System.Collections.Generic;
using BimosVrInjector.Core.Config;
using BimosVrInjector.Core.Resolve;
using BimosVrInjector.Mod.Authoring;
using BimosVrInjector.Mod.Runtime;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Panels;

namespace BimosVrInjector.Mod.UI
{
    internal sealed class AuthorPanel : PanelBase
    {
        public AuthorPanel(UIBase owner) : base(owner) { }

        public override string Name => "BIMOS VR Injector — Author";
        public override int MinWidth => 420;
        public override int MinHeight => 560;
        public override Vector2 DefaultAnchorMin => new Vector2(0.02f, 0.08f);
        public override Vector2 DefaultAnchorMax => new Vector2(0.30f, 0.92f);
        public override bool CanDragAndResize => true;

        private const int MaxRows = 100;

        private AuthorSession Session => ModEntry.Instance.Author;

        private Text _sceneLabel = null!;
        private Text _breadcrumbLabel = null!;
        private Text _selectionLabel = null!;
        private Text _rigLabel = null!;
        private Text _statusLabel = null!;

        private GameObject _listContent = null!;
        private readonly List<GameObject> _rowObjects = new List<GameObject>();

        protected override void ConstructPanelContent()
        {
            var root = UIFactory.CreateVerticalGroup(ContentRoot, "AuthorRoot",
                forceWidth: true, forceHeight: false, childControlWidth: true, childControlHeight: true,
                spacing: 4, padding: new Vector4(6, 6, 6, 6));
            UIFactory.SetLayoutElement(root, flexibleWidth: 9999, flexibleHeight: 9999);

            _sceneLabel = UIFactory.CreateLabel(root, "Scene", "Scene: —", TextAnchor.MiddleLeft, Color.white, true, 15);
            UIFactory.SetLayoutElement(_sceneLabel.gameObject, minHeight: 24, flexibleWidth: 9999);
            _breadcrumbLabel = UIFactory.CreateLabel(root, "Breadcrumb", "/", TextAnchor.MiddleLeft, new Color(0.7f, 0.85f, 1f));
            UIFactory.SetLayoutElement(_breadcrumbLabel.gameObject, minHeight: 20, flexibleWidth: 9999);

            var nav = UIFactory.CreateHorizontalGroup(root, "Nav", true, false, true, false, 4,
                default, new Color(0.12f, 0.12f, 0.12f));
            UIFactory.SetLayoutElement(nav, minHeight: 28, flexibleWidth: 9999);
            AddButton(nav, "Up", "◄ Up", () => { Session.GoUp(); RefreshAll(); });
            AddButton(nav, "Enter", "Enter ▶", () => { Session.EnterSelected(); RefreshAll(); });
            AddButton(nav, "Refresh", "Refresh", () => { Session.RefreshScene(); RefreshAll(); });

            var scroll = UIFactory.CreateScrollView(root, "Browser", out _listContent, out _,
                new Color(0.09f, 0.09f, 0.09f));
            UIFactory.SetLayoutElement(scroll, minHeight: 220, flexibleHeight: 9999, flexibleWidth: 9999);

            _selectionLabel = UIFactory.CreateLabel(root, "Selection", "Selected: (none)", TextAnchor.UpperLeft,
                Color.white, true, 13);
            UIFactory.SetLayoutElement(_selectionLabel.gameObject, minHeight: 44, flexibleWidth: 9999);

            var acts = UIFactory.CreateHorizontalGroup(root, "Actions", true, false, true, false, 4);
            UIFactory.SetLayoutElement(acts, minHeight: 30, flexibleWidth: 9999);
            AddButton(acts, "Disable", "Toggle Disable", () => { Session.ToggleDisableSelected(); RefreshAll(); });
            AddButton(acts, "Delete", "Toggle Delete", () => { Session.ToggleDeleteSelected(); RefreshAll(); });
            AddButton(acts, "Grab", "Toggle Grabbable", () => { Session.ToggleGrabbableSelected(); RefreshAll(); });

            var autoRow = UIFactory.CreateHorizontalGroup(root, "AutoGrab", true, false, true, false, 4);
            UIFactory.SetLayoutElement(autoRow, minHeight: 28, flexibleWidth: 9999);
            AddButton(autoRow, "AutoGrabToggle", "Auto-grab ALL bodies", () =>
            {
                Session.Log.ToggleAutoGrabAll();
                RefreshAll();
            });
            AddButton(autoRow, "AutoGrabNow", "Apply now", () =>
            {
                var n = ModEntry.Instance.AutoTagAllBodies();
                SetStatus($"Auto-grab tagged {n} bodies.");
            });

            _rigLabel = UIFactory.CreateLabel(root, "Rig", "Rig: (not set)", TextAnchor.MiddleLeft,
                new Color(0.8f, 1f, 0.8f));
            UIFactory.SetLayoutElement(_rigLabel.gameObject, minHeight: 22, flexibleWidth: 9999);
            var rigRow = UIFactory.CreateHorizontalGroup(root, "RigRow", true, false, true, false, 4);
            UIFactory.SetLayoutElement(rigRow, minHeight: 30, flexibleWidth: 9999);
            AddButton(rigRow, "SpawnRig", "Spawn Rig @ Camera", () => { SpawnRigAtCamera(); });
            AddButton(rigRow, "ClearRig", "Clear Rig", () => { Session.Log.ClearRig(); RefreshAll(); });

            var save = UIFactory.CreateHorizontalGroup(root, "Save", true, false, true, false, 4);
            UIFactory.SetLayoutElement(save, minHeight: 32, flexibleWidth: 9999);
            AddButton(save, "Save", "💾 Save Config", () => { SaveConfig(); });
            AddButton(save, "Replay", "▶ Test Replay", () => { ModEntry.Instance.TestReplayCurrentScene(); RefreshAll(); });

            _statusLabel = UIFactory.CreateLabel(root, "Status", "", TextAnchor.UpperLeft,
                new Color(0.85f, 0.85f, 0.85f), true, 12);
            UIFactory.SetLayoutElement(_statusLabel.gameObject, minHeight: 40, flexibleWidth: 9999);

            RefreshAll();
        }

        public void SpawnRigAtCamera()
        {
            ModEntry.Instance.EnsureXrStarted();
            Session.SpawnRigAtCamera();
            RefreshAll();
        }

        public void SaveConfig()
        {
            var path = Session.Save();
            SetStatus($"Saved -> {path}");
        }

        public void PickUnderCursor()
        {
            var cam = CameraUtil.GetActiveCamera();
            if (cam == null) { SetStatus("Pick failed: no camera."); return; }

            var ray = cam.ScreenPointToRay(UniverseLib.Input.InputManager.MousePosition);
            Session.PickUnderRay(ray);
            RefreshAll();
        }

        public void RefreshAll()
        {
            _sceneLabel.text = $"Scene: <b>{Session.SceneName}</b>";
            _breadcrumbLabel.text = "/" + (Session.BrowseParent != null
                ? ObjectKey.From(Session.BrowseParent).Path
                : "");

            RefreshSelection();
            RefreshRig();
            RebuildList();
            RefreshStatus();
        }

        private void RefreshSelection()
        {
            if (Session.Selected == null)
            {
                _selectionLabel.text = "Selected: (none)";
                return;
            }

            var key = ObjectKey.From(Session.Selected);
            var comps = string.Join(", ", key.Components.ToArray());
            _selectionLabel.text =
                $"Selected: <b>{key.Name}</b>\n<color=#9fd>{key.Path}</color>\n[{comps}]";
        }

        private void RefreshRig()
        {
            var rig = Session.Log.Rig;
            if (rig == null)
            {
                _rigLabel.text = "Rig: (not set)";
                return;
            }
            _rigLabel.text = $"Rig: pos({rig.Pos[0]:0.##}, {rig.Pos[1]:0.##}, {rig.Pos[2]:0.##}) " +
                             $"rot({rig.Rot[0]:0.#}, {rig.Rot[1]:0.#}, {rig.Rot[2]:0.#})";
        }

        private void RefreshStatus()
        {
            var log = Session.Log;
            SetStatus($"disable:{log.DisableCount}  delete:{log.DeleteCount}  " +
                      $"grab:{log.GrabbableCount}  rig:{(log.HasRig ? "yes" : "no")}  " +
                      $"auto-grab:{(log.AutoGrabAllBodies ? "ON" : "off")}");
        }

        private void SetStatus(string msg)
        {
            var journal = Session.Log.Journal;
            var last = journal.Count > 0 ? journal[journal.Count - 1] : "";
            _statusLabel.text = $"{msg}\n<color=#aaa>{last}</color>";
        }

        private ButtonRef AddButton(GameObject parent, string name, string text, System.Action onClick)
        {
            var btn = UIFactory.CreateButton(parent, name, text);
            UIFactory.SetLayoutElement(btn.Component.gameObject, minHeight: 26, flexibleWidth: 9999);
            btn.OnClick += onClick;
            return btn;
        }

        private void RebuildList()
        {
            foreach (var go in _rowObjects)
                Object.Destroy(go);
            _rowObjects.Clear();

            var children = Session.CurrentChildren;
            int shown = 0;
            foreach (var child in children)
            {
                if (shown >= MaxRows)
                {
                    var more = UIFactory.CreateLabel(_listContent, "More",
                        $"… {children.Count - MaxRows} more (narrow down by entering a parent)",
                        TextAnchor.MiddleLeft, new Color(1f, 0.8f, 0.5f));
                    _rowObjects.Add(more.gameObject);
                    break;
                }

                var node = (UnityTreeNode)child;
                if (!node.IsAlive)
                    continue;
                var btn = UIFactory.CreateButton(_listContent, "Row", RowLabel(node));
                UIFactory.SetLayoutElement(btn.Component.gameObject, minHeight: 24, flexibleWidth: 9999);
                var captured = node;
                btn.OnClick += () => { Session.Select(captured); RefreshAll(); };
                _rowObjects.Add(btn.Component.gameObject);
                shown++;
            }
        }

        private string RowLabel(UnityTreeNode node)
        {
            var path = node.GetPath();
            var log = Session.Log;

            var marks = "";
            if (log.IsDisabled(path)) marks += "⛔";
            if (log.IsDeleted(path)) marks += "🗑";
            if (log.IsGrabbable(path)) marks += "✋";

            var childCount = node.Children.Count;
            var arrow = childCount > 0 ? $" <color=#888>({childCount})▸</color>" : "";
            var sel = ReferenceEquals(node, Session.Selected) ? "▶ " : "";
            return $"{sel}{marks}{node.Name}{arrow}";
        }
    }
}
