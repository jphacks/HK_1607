class ApplicationController < ActionController::Base
  protect_from_forgery with: :exception
  include SessionsHelper
  # ログイン画面へのリダイレクトチェックを全アクションの前処理に設定
  before_action :authenticate_user!

  private
    # カレントユーザがログイン済みでない場合ログイン画面へとリダイレクト
    def authenticate_user!
      if cookies.signed[:user_id]
        # ログイン済み
      else
        # ログインしてない
        redirect_to login_url
      end
    end
end
