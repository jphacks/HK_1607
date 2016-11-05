module SessionsHelper
  # ログイン処理
  def log_in(user)
    # セッションにユーザ情報を格納
    session[:user_id] = user.id
    session[:login_id] = user.login_id
    session[:teacher_flag] = user.teacher_flag
    session[:admin_flag] = user.admin_flag

    # クッキーの生成
    cookies.signed[:user_id] = {
      :value => user.id,
      # 有効期限を30日に設定
      :expires => 30.day.from_now,
    }
  end

  # カレントユーザの設定
  def current_user
    # カレントユーザが設定されていない場合はクッキーのユーザIDから検索して取得
    @current_user ||= User.find(cookies.signed[:user_id])
  end

  # カレントユーザがログイン済みか否か
  def logged_in?
    !current_user.nil?
  end
end
