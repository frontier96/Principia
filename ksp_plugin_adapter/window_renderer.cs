﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace principia {
namespace ksp_plugin_adapter {

// A helper class for scaling the UI.  Unlike the WindowRenderer below, it can
// be used for elements that are not windows.
internal class ScalingRenderer {
  protected ScalingRenderer() {
    // All dimensions are expressed in "units".  By default a unit is 25 pixels
    // but it can scale up or down based on the KSP UI scale.
    scale_ = GameSettings.UI_SCALE * GameSettings.UI_SCALE_APPS;
    unit_ = 25 * scale_;
  }

  protected UnityEngine.GUILayoutOption GUILayoutHeight(int units) {
    return UnityEngine.GUILayout.Height(unit_ * units);
  }

  protected UnityEngine.GUILayoutOption GUILayoutMinWidth(int units) {
    return UnityEngine.GUILayout.MinWidth(unit_ * units);
  }

  protected UnityEngine.GUILayoutOption GUILayoutWidth(int units) {
    return UnityEngine.GUILayout.Width(unit_ * units);
  }

  protected float Width(int units) {
    return unit_ * units;
  }

  protected UnityEngine.GUISkin MakeSkin(UnityEngine.GUISkin template) {
    UnityEngine.GUISkin skin = UnityEngine.Object.Instantiate(template);

    // Creating a dynamic font as is done below results in Unity producing
    // incorrect character bounds and everything looks ugly.  They even
    // "document" it in their source code, see
    // https://github.com/Unity-Technologies/UnityCsReference/blob/57f723ec72ca50427e5d17cad0ec123be2372f67/Modules/GraphViewEditor/Views/GraphView.cs#L262.
    // So here I am, sizing a pangram to get an idea of the size of things and
    // nudging pixels by hand.  It's the 90's, go for it!
    var pangram = new UnityEngine.GUIContent(
        "Portez ce vieux whisky au juge blond qui fume.");
    float button_height = skin.button.CalcHeight(pangram, width : 1000);
    float horizontal_slider_height =
        skin.horizontalSlider.CalcHeight(pangram, width : 1000);
    float horizontal_slider_thumb_height =
        skin.horizontalSliderThumb.CalcHeight(pangram, width : 1000);
    float label_height = skin.label.CalcHeight(pangram, width : 1000);
    float text_area_height = skin.textArea.CalcHeight(pangram, width : 1000);
    float text_field_height = skin.textField.CalcHeight(pangram, width : 1000);
    float toggle_height = skin.toggle.CalcHeight(pangram, width : 1000);
    UnityEngine.Debug.LogError(button_height + " " +
    horizontal_slider_height+" "+horizontal_slider_thumb_height+" "+
    label_height + " " +
    text_area_height+" "+toggle_height);

    skin.font = UnityEngine.Font.CreateDynamicFontFromOSFont(
                      skin.font.fontNames,
                      (int)(skin.font.fontSize * scale_));

    skin.button.alignment = UnityEngine.TextAnchor.MiddleCenter;
    skin.button.contentOffset =
        new UnityEngine.Vector2(0, -button_height * scale_ / 10);
    skin.button.fixedHeight = button_height * scale_;
    skin.horizontalSlider.fixedHeight = 21 * scale_;
    skin.horizontalSliderThumb.fixedHeight = 21 * scale_;
    skin.horizontalSliderThumb.fixedWidth = 12 * scale_;
    skin.label.alignment = UnityEngine.TextAnchor.MiddleLeft;
        skin.label.contentOffset =
            new UnityEngine.Vector2(0, -label_height * scale_ / 20);
        skin.label.fixedHeight = label_height * scale_;
    skin.textArea.alignment = UnityEngine.TextAnchor.MiddleLeft;
    skin.textArea.contentOffset =
        new UnityEngine.Vector2(0, -text_area_height * scale_ / 20);
    skin.textArea.fixedHeight = text_area_height * scale_;
    skin.textField.alignment = UnityEngine.TextAnchor.MiddleLeft;
    skin.textField.contentOffset =
        new UnityEngine.Vector2(0, -text_area_height * scale_ / 20);
    skin.textField.fixedHeight = text_field_height * scale_;
    skin.toggle.fixedHeight = toggle_height * scale_;
    skin.toggle.contentOffset =
        new UnityEngine.Vector2(0, -toggle_height * (scale_ - 1) / 3);
    skin.toggle.alignment = UnityEngine.TextAnchor.UpperLeft;
    skin.toggle.margin = new UnityEngine.RectOffset(
        (int)(skin.toggle.margin.left * scale_),
        skin.toggle.margin.right,
        (int)(skin.toggle.margin.top * 1.7 * scale_),
        skin.toggle.margin.bottom);
    return skin;
  }

  private readonly float scale_;
  private readonly float unit_;
}

internal abstract class BaseWindowRenderer : ScalingRenderer, IConfigNode {
  protected BaseWindowRenderer(UnityEngine.GUILayoutOption[] options)
      : base() {
    UnityEngine.GUILayoutOption[] default_options = {GUILayoutMinWidth(20)};
    options_ = options.Length == 0 ? default_options : options;
    lock_name_ = GetType().ToString() + ":lock:" + GetHashCode();
  }

  // Locking.

  protected void ClearLock() {
    InputLockManager.RemoveControlLock(lock_name_);
  }

  private void InputLock() {
    UnityEngine.Vector3 mouse = UnityEngine.Input.mousePosition;
    mouse.y = UnityEngine.Screen.height - mouse.y;
    if (rectangle_.Contains(mouse)) {
      InputLockManager.SetControlLock(PrincipiaLock, lock_name_);
    } else {
      InputLockManager.RemoveControlLock(lock_name_);
    }
  }

  // Rendering.

  public void RenderWindow() {
    var old_skin = UnityEngine.GUI.skin;
    if (skin_ == null) {
      UnityEngine.GUI.skin = null;
      skin_ = MakeSkin(UnityEngine.GUI.skin);
    }
    UnityEngine.GUI.skin = skin_;
    if (show_) {
      rectangle_ = UnityEngine.GUILayout.Window(
                       id         : this.GetHashCode(),
                       screenRect : rectangle_,
                       func       : RenderWindow,
                       text       : Title,
                       options    : options_);

      // The first time a window is shown, we have a moral duty to place it at
      // the centre of the screen.  This is tricky because we don't know its
      // width.  So we go through this method until the width has been
      // determined, and we update the window based on the width.  Note that
      // |must_centre_| has to be persisted because there can be multiple
      // scene changes because a window is shown for the first time.
      if (must_centre_ && rectangle_.width > 0) {
        rectangle_ =
            UnityEngine.GUILayout.Window(
                id         : this.GetHashCode(),
                screenRect : new UnityEngine.Rect(
                    x      : (UnityEngine.Screen.width - rectangle_.width) / 2,
                    y      : UnityEngine.Screen.height / 3,
                    width  : 0,
                    height : 0),
                func       : RenderWindow,
                text       : Title,
                options    : options_);
        must_centre_ = false;
      }
      EnsureOnScreen();
      InputLock();
    } else {
      ClearLock();
    }
    UnityEngine.GUI.skin = old_skin;
  }

  private void EnsureOnScreen() {
    rectangle_.x = UnityEngine.Mathf.Clamp(
                       rectangle_.x,
                       -rectangle_.width + min_width_on_screen_,
                       UnityEngine.Screen.width - min_width_on_screen_);
    rectangle_.y = UnityEngine.Mathf.Clamp(
                       rectangle_.y,
                       -rectangle_.height + min_height_on_screen_,
                       UnityEngine.Screen.height - min_height_on_screen_);
  }

  public void Shrink() {
    rectangle_.height = 0.0f;
    rectangle_.width = 0.0f;
  }

  // Visibility.

  public void Hide() {
    show_ = false;
  }

  public void Show() {
    show_ = true;
  }

  public bool Shown() {
    return show_;
  }

  public void Toggle() {
    show_ = !show_;
  }

  // Persistence.

  public virtual void Load(ConfigNode node) {
    String must_centre_value = node.GetAtMostOneValue("must_centre");
    if (must_centre_value != null) {
      must_centre_ = Convert.ToBoolean(must_centre_value);
    }
    String show_value = node.GetAtMostOneValue("show");
    if (show_value != null) {
      show_ = Convert.ToBoolean(show_value);
    }
    String x_value = node.GetAtMostOneValue("x");
    if (x_value != null) {
      rectangle_.x = Convert.ToSingle(x_value);
    }
    String y_value = node.GetAtMostOneValue("y");
    if (y_value != null) {
      rectangle_.y = Convert.ToSingle(y_value);
    }
  }

  public virtual void Save(ConfigNode node) {
    node.SetValue("must_centre", must_centre_, createIfNotFound : true);
    node.SetValue("show", show_, createIfNotFound : true);
    node.SetValue("x", rectangle_.x, createIfNotFound : true);
    node.SetValue("y", rectangle_.y, createIfNotFound : true);
  }

  abstract protected String Title { get; }
  abstract protected void RenderWindow(int window_id);

  private static readonly ControlTypes PrincipiaLock =
      ControlTypes.ALLBUTCAMERAS &
      ~ControlTypes.ALL_SHIP_CONTROLS;

  private const float min_height_on_screen_ = 50;
  private const float min_width_on_screen_ = 50;

  private readonly UnityEngine.GUILayoutOption[] options_;
  private readonly String lock_name_;
  private UnityEngine.GUISkin skin_;
  private bool must_centre_ = true;
  private bool show_ = false;
  private UnityEngine.Rect rectangle_;
}

internal abstract class SupervisedWindowRenderer : BaseWindowRenderer {
  public interface ISupervisor {
    event Action clear_locks;
    event Action dispose_windows;
    event Action render_windows;
  }

  public SupervisedWindowRenderer(ISupervisor supervisor,
                                  params UnityEngine.GUILayoutOption[] options)
        : base(options) {
    supervisor_ = supervisor;
    supervisor_.clear_locks += ClearLock;
    supervisor_.dispose_windows += DisposeWindow;
    supervisor_.render_windows += RenderWindow;
  }

  public void DisposeWindow() {
    supervisor_.clear_locks -= ClearLock;
    supervisor_.dispose_windows -= DisposeWindow;
    supervisor_.render_windows -= RenderWindow;
  }

  private ISupervisor supervisor_;
}

internal abstract class UnsupervisedWindowRenderer : BaseWindowRenderer {
  public UnsupervisedWindowRenderer(
      params UnityEngine.GUILayoutOption[] options) : base(options) {}
}

}  // namespace ksp_plugin_adapter
}  // namespace principia
