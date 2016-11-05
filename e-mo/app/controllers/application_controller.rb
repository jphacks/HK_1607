class ApplicationController < ActionController::Base
  protect_from_forgery with: :exception
  include SessionsHelper
  before_action :authenticate_user!

  private
    def authenticate_user!
      if cookies.signed[:user_id]
        # ログイン済み
      else
        # ログインしてない
        redirect_to login_url
      end
    end
end]
