class StudentsController < ApplicationController
  # アクセスフィルターを本コントローラのアクション前処理に設定
  before_action :access_filter

  # GET /student
  # GET /
  def index
    @login_id = current_user.login_id
  end

  private
    # カレントユーザでの生徒画面へのアクセスフィルター
    def access_filter
      # 先生でログインしている場合
      if current_user.teacher_flag
        # 先生画面へリダイレクト
        redirect_to teacher_url
      # 生徒でログインしている場合
      else
        # ...
      end
    end
end
